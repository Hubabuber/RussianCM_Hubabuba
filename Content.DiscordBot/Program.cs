using System.Text.Json;
using Content.DiscordBot;
using Content.Server.Database;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents =
        GatewayIntents.Guilds |
        GatewayIntents.GuildMessages |
        GatewayIntents.MessageContent,
});
client.Log += Logger.Log;
var seedBoostyTiers = args.Contains("--seed-boosty-tiers");
var listBoostyTiers = args.Contains("--list-boosty-tiers");
var listTestPatrons = args.Contains("--list-test-patrons");
var grantTestTierIndex = Array.IndexOf(args, "--grant-test-tier");

string? token = null;
string? connectionString = null;
var databaseEngine = "postgres";
var guild = 0UL;
if (File.Exists("config.json"))
{
    var config = await JsonSerializer.DeserializeAsync<Config>(File.OpenRead("config.json")) ?? new Config();
    token = config.Token;
    connectionString = config.DatabaseString;
    databaseEngine = config.DatabaseEngine;
    guild = config.Guild;
}

#if DEBUG
if (Environment.GetEnvironmentVariable("DISCORD_TOKEN") is { } envToken)
    token = envToken;

if (Environment.GetEnvironmentVariable("DATABASE_STRING") is { } dbString)
    connectionString = dbString;

if (Environment.GetEnvironmentVariable("DATABASE_ENGINE") is { } dbEngine)
    databaseEngine = dbEngine;

if (Environment.GetEnvironmentVariable("DISCORD_GUILD") is { } guildString &&
    ulong.TryParse(guildString, out var envGuild))
{
    guild = envGuild;
}
#endif

if (string.IsNullOrWhiteSpace(connectionString))
    throw new ArgumentException("No database connection string found.");

ServerDbContext CreateConfiguredDatabase()
{
    if (databaseEngine.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
    {
        raw.SetProvider(new SQLite3Provider_e_sqlite3());
        var sqliteBuilder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        sqliteBuilder.UseSqlite(connectionString);
        return new SqliteServerDbContext(sqliteBuilder.Options);
    }

    var postgresBuilder = new DbContextOptionsBuilder<PostgresServerDbContext>();
    postgresBuilder.UseNpgsql(connectionString);
    return new PostgresServerDbContext(postgresBuilder.Options);
}

async Task WithConfiguredDatabase(Func<ServerDbContext, Task> action)
{
    await using var db = CreateConfiguredDatabase();
    await action(db);
}

if (seedBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.Seed);
    Console.WriteLine("Boosty sponsor tiers seeded.");
    return;
}

if (listBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintTiers);
    return;
}

if (listTestPatrons)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintPatrons);
    return;
}

if (grantTestTierIndex >= 0)
{
    if (args.Length <= grantTestTierIndex + 2)
        throw new ArgumentException("Usage: --grant-test-tier <player-name-or-user-id> <tier-name>");

    var playerNameOrId = args[grantTestTierIndex + 1];
    var tierName = args[grantTestTierIndex + 2];
    await WithConfiguredDatabase(db => BoostyTierSeeder.GrantTestTier(db, playerNameOrId, tierName));
    Console.WriteLine($"Granted '{tierName}' to '{playerNameOrId}'.");
    return;
}

if (string.IsNullOrWhiteSpace(token))
    throw new ArgumentException("No token found.");

if (guild == 0)
    throw new ArgumentException("No Discord guild found.");

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

await using var db = CreateConfiguredDatabase();
// await db.Database.MigrateAsync();

var interaction = new InteractionService(client);
var handler = new CommandHandler(client, new CommandService(), interaction, db, guild);

AppDomain.CurrentDomain.ProcessExit += (_, _) => Interlocked.Decrement(ref handler.Running);

await handler.InstallCommandsAsync();

// Block this task until the program is closed.
await Task.Delay(-1);
