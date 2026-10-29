using System;
using System.Linq;
using Content.Client.Eui;
using Content.Client._RMC14.LinkAccount;
using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client._RuCM.Kaban;

// CMU14: private server-authoritative account UI.
[UsedImplicitly]
public sealed class KabanEui : BaseEui
{
    private readonly KabanWindow _window;
    public KabanEui()
    {
        _window = new(request => SendMessage(request));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }
    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Dispose();
    public override void HandleState(EuiStateBase state)
    {
        if (state is KabanState view)
            _window.Update(view);
    }
}

public sealed partial class KabanEntrySystem : EntitySystem
{
    public Button AttachPersonalization(Control parent)
    {
        if (parent.Children.OfType<Button>().FirstOrDefault(c => c.Name == "RuCMKabanEntry") is { } existing)
            return existing;
        var button = new Button { Name = "RuCMKabanEntry", Text = Loc.GetString("cmu-kaban-title"), HorizontalExpand = true, MinHeight = 34 };
        button.AddStyleClass(StyleNano.StyleClassCrtButton);
        button.OnPressed += _ => RaiseNetworkEvent(new KabanOpenEvent());
        parent.AddChild(button);
        return button;
    }
}
