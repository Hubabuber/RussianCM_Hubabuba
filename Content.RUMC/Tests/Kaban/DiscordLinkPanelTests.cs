using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Content.Server._RuCM.Discord;
using Discord;

namespace Content.IntegrationTests._RuCM.Kaban;

public sealed class DiscordLinkPanelTests
{
    public class InterfaceProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Call = default!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!, args);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> call) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceProxy>();
        ((InterfaceProxy) (object) proxy).Call = call;
        return proxy;
    }

    private sealed class Channel
    {
        public readonly List<IMessage> Messages = new();
        public int Created;
        public int Modified;
        public bool ReadFailure;
        public readonly IMessageChannel Api;

        public Channel()
        {
            Api = Proxy<IMessageChannel>((method, args) => method.Name switch
            {
                "GetMessagesAsync" => History(),
                "SendMessageAsync" => Send(),
                _ => throw new NotSupportedException(method.Name),
            });
        }

        public IUserMessage Message(ulong author, string component)
        {
            var user = Proxy<IUser>((method, _) => method.Name == "get_Id" ? author : throw new NotSupportedException(method.Name));
            var components = new ComponentBuilder().WithButton("Link", component).Build().Components;
            return Proxy<IUserMessage>((method, _) => method.Name switch
            {
                "get_Author" => user,
                "get_Components" => components,
                "ModifyAsync" => Modify(),
                _ => throw new NotSupportedException(method.Name),
            });
        }

        private async IAsyncEnumerable<IReadOnlyCollection<IMessage>> History()
        {
            await Task.Yield();
            if (ReadFailure) throw new IOException("Test: history is unavailable.");
            yield return Messages.ToArray();
        }

        private Task<IUserMessage> Send()
        {
            Created++;
            var message = Message(42, DiscordLinkPanel.Component);
            Messages.Add(message);
            return Task.FromResult(message);
        }

        private Task Modify() { Modified++; return Task.CompletedTask; }
    }

    [Test]
    public async Task RepeatedPublicationReusesOwnPanelAndSkipsOtherBotsAndButtons()
    {
        var channel = new Channel();
        channel.Messages.Add(channel.Message(11, DiscordLinkPanel.Component));
        channel.Messages.Add(channel.Message(42, "unrelated"));
        var a = await DiscordLinkPanel.Publish(channel.Api, 42, "Text", "Link", new RequestOptions());
        var b = await DiscordLinkPanel.Publish(channel.Api, 42, "Updated", "Link", new RequestOptions());
        Assert.That(b, Is.SameAs(a));
        Assert.That(channel.Created, Is.EqualTo(1));
        Assert.That(channel.Modified, Is.EqualTo(1));
    }

    [Test]
    public void HistoryFailureCannotPublishSpeculativeDuplicate()
    {
        var channel = new Channel { ReadFailure = true };
        Assert.ThrowsAsync<IOException>(() => DiscordLinkPanel.Publish(channel.Api, 42, "Text", "Link", new RequestOptions()));
        Assert.That(channel.Created, Is.Zero);
        Assert.That(channel.Modified, Is.Zero);
    }
}
