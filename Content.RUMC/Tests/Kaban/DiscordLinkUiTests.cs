using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Content.Client._RMC14.LinkAccount;
using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Corvax.CCCVars;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._RuCM.Kaban;

[TestFixture, NonParallelizable]
public sealed class DiscordLinkUiTests : GameTest
{
    private static int _ttsOverrideIndex;
    public override PoolSettings PoolSettings => new() { InLobby = true, Fresh = true, Destructive = true };

    [OneTimeSetUp]
    public static void ConfigureLobby()
    {
        // Lobby join announcements precede per-test overrides; this UI fixture has no external TTS provider.
        _ttsOverrideIndex = PoolManager.Instance.DefaultCvars.Count;
        PoolManager.Instance.DefaultCvars.Add((CCCVars.TTSEnabled.Name, "false"));
    }

    [OneTimeTearDown]
    public static void RestoreLobby() => PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex);

    [Test]
    public async Task SettingsOpenLinkingAndPersonalizationOwnsAccountActionsWhileLobbyRemainsClear()
    {
        Guid code = default;
        Guid user = default;
        LinkAccountWindow window = default!;
        TestContext.Progress.WriteLine("Discord UI: entering connected lobby");
        await Server.WaitPost(() => Server.CfgMan.SetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink, ""));
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            Assert.That(ui.ActiveScreen, Is.TypeOf<LobbyGui>());
            var lobby = (LobbyGui) ui.ActiveScreen!;
            var roundActions = lobby.ObserveButton.Parent!;
            Assert.That(Descendants(roundActions).Any(c => c.Name is "RuCMKabanEntry" or "RuCMDiscordLinkEntry"
                or "RuCMQualificationLobby" or "RuCMQualificationPersonalization"), Is.False);
            Click(lobby.CharacterPreview.CharacterSetupButton);
            var personalization = Descendants(lobby).OfType<CharacterSetupGui>().Single();
            Assert.That(personalization.VisibleInTree, Is.True);
            var account = personalization.AccountActions;
            Assert.That(Descendants(account).Count(c => c.Name == "RuCMKabanEntry"), Is.EqualTo(1));
            Assert.That(Descendants(account).Count(c => c.Name == "RuCMQualificationPersonalization"), Is.EqualTo(1));
            account.Measure(new Vector2(325, 100));
            account.Arrange(new UIBox2(Vector2.Zero, new Vector2(325, 100)));
            Assert.That(account.DesiredSize.X, Is.LessThanOrEqualTo(325));
            using var options = ui.CreateWindow<OptionsMenu>();
            options.OpenCentered();
            var settingsSize = new Vector2(1000, 700);
            options.SetSize = settingsSize;
            options.Measure(settingsSize);
            options.Arrange(new UIBox2(Vector2.Zero, settingsSize));
            var entry = Descendants(options).OfType<Button>().Single(b => b.Name == "DiscordLinkButton");
            Assert.That(entry.VisibleInTree, Is.True);
            Assert.That(entry.Size.X > 2 && entry.Size.Y > 2, Is.True);
            user = Client.ResolveDependency<IPlayerManager>().LocalSession!.UserId.UserId;
            Client.ResolveDependency<Content.Client._RMC14.LinkAccount.LinkAccountManager>().CodeReceived += received => code = received;
            Click(entry);
            options.Close();
            window = Descendants(ui.WindowRoot).OfType<LinkAccountWindow>().Single();
            Assert.That(window.CopyButton.Disabled, Is.True);
            Assert.That(window.LinkButton.Visible, Is.True);
            Assert.That(window.LinkButton.Disabled, Is.True);
        });
        TestContext.Progress.WriteLine("Discord UI: waiting for code response");
        await PoolManager.WaitUntil(Server, async () =>
        {
            // Lobby sessions need raw-message ticks, not an entity-state acknowledgement.
            await Pair.RunTicksSync(5);
            var ready = false;
            await Client.WaitPost(() => ready = code != Guid.Empty);
            return ready;
        }, maxTicks: 1200, tickStep: 5);
        Task<Guid?> stored = default!;
        await Server.WaitPost(() => stored = Server.ResolveDependency<IServerDbManager>().GetLinkingCode(user));
        await PoolManager.WaitUntil(Server, () => stored.IsCompleted);
        Assert.That(await stored, Is.EqualTo(code), "The displayed code must already exist in the game database.");
        await Client.WaitAssertion(() => Assert.That(window.CopyButton.Disabled, Is.False));

        TestContext.Progress.WriteLine("Discord UI: publishing channel URL after the window opened");
        await Server.WaitPost(() => Server.CfgMan.SetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink,
            "https://discord.com/channels/1/2/3"));
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() => Assert.That(window.LinkButton.Disabled, Is.False));
        await Server.WaitPost(() => Server.CfgMan.SetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink, ""));
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(window.LinkButton.Disabled, Is.True);
            Assert.That(window.LinkButton.Visible, Is.True);
            window.Close();
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            using var escape = ui.CreateWindow<EscapeMenu>();
            escape.OpenCentered();
            Assert.That(Descendants(escape).Any(c => c.Name is "RuCMDiscordLinkEntry" or "RuCMKabanEntry"), Is.False);
            escape.Close();
            escape.OpenCentered();
            Assert.That(Descendants(escape).Any(c => c.Name is "RuCMDiscordLinkEntry" or "RuCMKabanEntry"), Is.False);
            escape.Close();
            Client.ResolveDependency<IClientConsoleHost>().ExecuteCommand("discordlink");
            var reopened = Descendants(ui.WindowRoot).OfType<LinkAccountWindow>().Single();
            Assert.That(reopened, Is.Not.SameAs(window));
            Assert.That(reopened.CopyButton.Disabled, Is.True, "Do not offer the previous window's code while requesting a new one.");
            reopened.Close();
        });
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (var child in parent.Children)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void Click(BaseButton button)
    {
        var pointer = new ScreenCoordinates(button.GlobalPixelPosition + new Vector2(2, 2), default);
        var down = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, pointer, true, new(2, 2), new(2, 2));
        var up = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, pointer, true, new(2, 2), new(2, 2));
        typeof(BaseButton).GetMethod("KeyBindDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { down });
        typeof(BaseButton).GetMethod("KeyBindUp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { up });
    }
}
