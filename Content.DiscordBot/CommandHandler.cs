using System.Collections.Immutable;
using System.Reflection;
using Content.DiscordBot.Modules;
using Content.Server.Database;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot;

public sealed class CommandHandler(
    DiscordSocketClient client,
    CommandService commands,
    InteractionService interaction,
    Func<ServerDbContext> databaseFactory,
    ulong guild)
{
    private ImmutableDictionary<ulong, RMCPatronTier>? _patronTiers;
    private ImmutableArray<RMCPatronTier> _tierPriority;
    private Task? _refreshPatronsTask;

    private sealed record LinkedPatronSnapshot(Guid PlayerId, ulong DiscordId, string PlayerName);
    private sealed record PatronRefreshDecision(
        Guid PlayerId,
        ulong DiscordId,
        string PlayerName,
        string? DiscordUsername,
        int? TierId,
        string? TierName);

    public int Running = 1;

    public async Task InstallCommandsAsync()
    {
        await using var db = databaseFactory();
        var patronTiers = await db.RMCPatronTiers.ToListAsync();
        _tierPriority = [..patronTiers.OrderBy(t => t.Priority)];
        _patronTiers = patronTiers.ToImmutableDictionary(t => t.DiscordRole, t => t);

        client.MessageReceived += HandleCommandAsync;
        client.ButtonExecuted += HandleButtonAsync;
        client.ModalSubmitted += HandleModalAsync;
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        interaction.AddModalInfo<LinkAccountModal>();

        _refreshPatronsTask = Task.Run(async () => await RefreshPatrons());
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Don't process the command if it was a system message
        var message = messageParam as SocketUserMessage;
        if (message == null)
            return;

        // Create a number to track where the prefix ends and the command begins
        var argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
            return;

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(client, message);

        // Execute the command with the command context we just created.
        var result = await commands.ExecuteAsync(context, argPos, null);
        if (!result.IsSuccess)
        {
            var reason = result.ErrorReason ?? "Unknown command error";
            await Logger.Info($"Command '{message.Content}' failed for {message.Author.Username}: {reason}");

            if (result.Error != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync($"Command failed: {reason}");
        }
    }

    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        switch (component.Data.CustomId)
        {
            case "link-ss14-account":
                await component.RespondWithModalAsync<LinkAccountModal>("link-ss14-account");
                break;
        }
    }

    private async Task HandleModalAsync(SocketModal modal)
    {
        switch (modal.Data.CustomId)
        {
            case "link-ss14-account":
            {
                if (modal.GuildId is not { } guildId)
                    break;

                await using var db = databaseFactory();

                var codeStr = modal.Data.Components.First(c => c.CustomId == "account_code").Value.Trim();
                if (string.IsNullOrWhiteSpace(codeStr))
                    break;

                await modal.DeferAsync(true);
                if (!Guid.TryParse(codeStr, out var code))
                {
                    await modal.FollowupAsync($"{codeStr} isn't a valid code! Get one in-game from the lobby at the top left of the screen.", ephemeral: true);
                }

                var author = modal.User;
                var authorId = author.Id;
                var discord = await db.RMCDiscordAccounts
                    .Include(d => d.LinkedAccount)
                    .ThenInclude(l => l.Player)
                    .ThenInclude(p => p.Patron)
                    .FirstOrDefaultAsync(a => a.Id == authorId);
                var codes = await db.RMCLinkingCodes
                    .Include(l => l.Player)
                    .ThenInclude(player => player.Patron)
                    .FirstOrDefaultAsync(p => p.Code == code);

                if (codes == null)
                {
                    await modal.FollowupAsync($"No player found with code {codeStr}, join the game server and get another code before trying again, or ask for help in another channel.", ephemeral: true);
                    break;
                }

                if (codes.CreationTime < DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
                {
                    await modal.FollowupAsync($"Code {codeStr} were generated too long ago, join the game server and get another code before trying again.", ephemeral: true);
                }

                if (discord?.LinkedAccount is { } linked)
                {
                    if (linked.Player.Patron is { } patron)
                        db.RMCPatrons.Remove(patron);

                    linked.Player.Patron = null;
                    db.RMCLinkedAccounts.Remove(linked);
                }

                discord ??= db.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = authorId }).Entity;
                discord.LinkedAccount = db.RMCLinkedAccounts.Add(new RMCLinkedAccount { Discord = discord }).Entity;
                discord.LinkedAccount.Player = codes.Player;

                var member = await client.Rest.GetGuildUserAsync(guildId, authorId);
                var roles = member?.RoleIds.ToArray() ?? [];
                var selectedTier = await db.RMCPatronTiers
                    .Where(t => roles.Contains(t.DiscordRole))
                    .OrderBy(t => t.Priority)
                    .FirstOrDefaultAsync();

                db.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
                {
                    Discord = discord,
                    Player = discord.LinkedAccount.Player,
                });

                await using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    db.ChangeTracker.DetectChanges();
                    await db.SaveChangesAsync();
                    if (selectedTier == null)
                        await RMCPatronPersistence.RemoveAsync(db, codes.Player.UserId);
                    else
                        await RMCPatronPersistence.SetTierAsync(db, codes.Player.UserId, selectedTier.Id);
                    await transaction.CommitAsync();
                }

                var msg = $"Linked SS14 account with name {codes.Player.LastSeenUserName}";
                if (selectedTier != null)
                    msg += $" and tier {selectedTier.Name}";

                await modal.FollowupAsync(msg, ephemeral: true);
                break;
            }
        }
    }

    private async Task RefreshPatrons()
    {
        while (Interlocked.CompareExchange(ref Running, 1, 1) == 1)
        {
            try
            {
                List<LinkedPatronSnapshot> patrons;
                await using (var readDb = databaseFactory())
                {
                    patrons = await readDb.RMCLinkedAccounts
                        .AsNoTracking()
                        .Select(linked => new LinkedPatronSnapshot(
                            linked.PlayerId,
                            linked.DiscordId,
                            linked.Player.LastSeenUserName))
                        .ToListAsync();
                }

                var decisions = new List<PatronRefreshDecision>();
                foreach (var linked in patrons)
                {
                    try
                    {
                        var user = await client.Rest.GetGuildUserAsync(guild, linked.DiscordId);
                        if (user == null)
                        {
                            decisions.Add(new PatronRefreshDecision(
                                linked.PlayerId,
                                linked.DiscordId,
                                linked.PlayerName,
                                null,
                                null,
                                null));
                            continue;
                        }

                        var tier = _tierPriority.FirstOrDefault(value => user.RoleIds.Contains(value.DiscordRole));
                        decisions.Add(new PatronRefreshDecision(
                            linked.PlayerId,
                            linked.DiscordId,
                            linked.PlayerName,
                            user.Username,
                            tier?.Id,
                            tier?.Name));
                    }
                    catch (Exception e)
                    {
                        await Logger.Error($"Error updating patron with discord id {linked.DiscordId} and player id {linked.PlayerId}", e);
                    }
                }

                var changes = new List<PatronRefreshDecision>();
                await using (var writeDb = databaseFactory())
                await using (var transaction = await writeDb.Database.BeginTransactionAsync())
                {
                    foreach (var decision in decisions)
                    {
                        var changed = decision.TierId == null
                            ? await RMCPatronPersistence.RemoveAsync(writeDb, decision.PlayerId)
                            : await RMCPatronPersistence.SetTierAsync(
                                writeDb,
                                decision.PlayerId,
                                decision.TierId.Value);
                        if (changed)
                            changes.Add(decision);
                    }

                    await transaction.CommitAsync();
                }

                foreach (var change in changes)
                {
                    if (change.TierId != null)
                    {
                        await Logger.Info(
                            $"Updated patron {change.DiscordUsername}:{change.DiscordId}:{change.PlayerName} " +
                            $"with tier {change.TierName}");
                    }
                    else if (change.DiscordUsername == null)
                    {
                        await Logger.Info($"Removed patron {change.DiscordId}:{change.PlayerName}");
                    }
                    else
                    {
                        await Logger.Info(
                            $"Removed patron {change.DiscordUsername}:{change.DiscordId}:{change.PlayerName}");
                    }
                }
            }
            catch (Exception e)
            {
                await Logger.Error("Error refreshing patrons", e);
            }

            await Task.Delay(60000);
        }
    }
}
