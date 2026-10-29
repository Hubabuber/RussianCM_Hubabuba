using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Content.Server._RuCM.Discord;

public static class DiscordLinkPanel
{
    public const string Component = "link-ss14-account";

    public static async Task<IUserMessage> Publish(IMessageChannel channel, ulong bot,
        string text, string button, RequestOptions options)
    {
        IUserMessage? panel = null;
        // Search lazily, stopping at our first panel. A failed search never creates a speculative duplicate.
        await foreach (var batch in channel.GetMessagesAsync(int.MaxValue, options: options))
        {
            panel = batch.OfType<IUserMessage>().FirstOrDefault(m => m.Author.Id == bot
                && m.Components.OfType<ActionRowComponent>().SelectMany(r => r.Components)
                    .OfType<ButtonComponent>().Any(b => b.CustomId == Component));
            if (panel != null)
                break;
        }
        var components = new ComponentBuilder().WithButton(button, Component).Build();
        if (panel == null)
            return await channel.SendMessageAsync(text, components: components, options: options,
                allowedMentions: AllowedMentions.None);
        await panel.ModifyAsync(p => { p.Content = text; p.Components = components; }, options);
        return panel;
    }
}
