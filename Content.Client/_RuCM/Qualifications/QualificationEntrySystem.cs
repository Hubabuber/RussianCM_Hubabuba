using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.ResourceManagement;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.LateJoin;
using Content.Client.Options.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._RuCM.Qualifications;

/// <summary>Compose public controls without replacing upstream lobby or Escape controllers.</summary>
public sealed partial class QualificationEntrySystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private QualificationRolePolicy _policy = default!;
    [Dependency] private JobRequirementsManager _requirements = default!;
    [Dependency] private IClientPreferencesManager _preferences = default!;
    private Guid _session;
    private readonly HashSet<EscapeMenu> _escapes = new();
    private bool _staff;
    private bool _refreshRequirements;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<QualificationPolicyState>(ReceivePolicy);
        _ui.WindowRoot.OnChildAdded += OnWindowAdded;
        _ui.WindowRoot.OnChildRemoved += OnWindowRemoved;
    }
    public override void Shutdown()
    {
        _ui.WindowRoot.OnChildAdded -= OnWindowAdded;
        _ui.WindowRoot.OnChildRemoved -= OnWindowRemoved;
        _escapes.Clear(); base.Shutdown();
    }
    private void OnWindowAdded(Control control)
    {
        if (control is not EscapeMenu escape) return;
        _escapes.Add(escape); AttachEscape(escape, _staff, OpenRecord);
    }
    private void OnWindowRemoved(Control control)
    { if (control is EscapeMenu escape) _escapes.Remove(escape);
    }
    private void ReceivePolicy(QualificationPolicyState state)
    {
        _staff = state.Staff;
        _policy.Eligibility.Clear();
        foreach (var (job, allowed) in state.Eligibility) _policy.Eligibility[job] = allowed;
        _policy.Apply(state.Active, state.Jobs, true, state.Baseline, state.BaseRequirements);
        _refreshRequirements = true;
        foreach (var escape in _escapes) if (!escape.Disposed) AttachEscape(escape, _staff, OpenRecord);
    }
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var session = _players.LocalSession;
        if (session == null) { _session = Guid.Empty; _staff = false; return; }
        if (_session != (Guid) session.UserId)
        { _session = session.UserId; RaiseNetworkEvent(new QualificationPolicyRequest()); }
        if (!_refreshRequirements) return;
        _refreshRequirements = false;
        foreach (var editor in Descendants(_ui.RootControl).OfType<HumanoidProfileEditor>()) editor.RefreshJobs();
        foreach (var button in Descendants(_ui.WindowRoot).OfType<JobButton>())
        {
            if (!ProtoMan.TryIndex<JobPrototype>(button.JobId, out var job)) continue;
            button.Disabled = !_requirements.IsAllowed(job, (HumanoidCharacterProfile?) _preferences.Preferences?.SelectedCharacter, out var reason);
            if (button.Disabled && reason != null)
            { var tooltip = new Tooltip(); tooltip.SetMessage(reason); button.TooltipSupplier = _ => tooltip; }
            else button.TooltipSupplier = null;
            // Retain the upstream job icon; hide its old lock when certification unlocks the button.
            foreach (var texture in Descendants(button).OfType<TextureRect>().Where(t =>
                t.TextureScale == new System.Numerics.Vector2(0.4f, 0.4f) && t.HorizontalAlignment == Control.HAlignment.Right))
                texture.Visible = button.Disabled;
        }
    }
    private void OpenRecord() => RaiseNetworkEvent(new QualificationEntryRequest());
    public static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children.ToArray())
        { yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
    }
    // CMU14: personal records belong to character personalization, not the round action column.
    public Button AttachPersonalization(Control parent) => AttachPersonalization(parent, OpenRecord);
    public static Button AttachPersonalization(Control parent, Action open)
        => AddButton(parent, "RuCMQualificationPersonalization", "lobby-record", open);
    public static Button AttachEscape(EscapeMenu escape, bool staff, Action open)
    {
        var parent = escape.RulesButton.Parent!;
        var button = AddButton(parent, "RuCMQualificationEscape", "staff-records", () => { escape.Close(); open(); });
        button.Visible = staff;
        button.SetPositionInParent(escape.AdminRemarksButton.GetPositionInParent() + 1);
        return button;
    }
    private static Button AddButton(Control parent, string name, string key, Action open)
    {
        if (parent.Children.OfType<Button>().FirstOrDefault(b => b.Name == name) is { } existing) return existing;
        var button = new Button { Name = name, Text = Robust.Shared.Localization.Loc.GetString("rucm-qualifications-" + key),
            ToolTip = Robust.Shared.Localization.Loc.GetString("rucm-qualifications-" + key + "-help"), HorizontalExpand = true, MinHeight = 38 };
        button.Label.FontOverride = Robust.Shared.IoC.IoCManager.Resolve<Robust.Client.ResourceManagement.IResourceCache>().NotoStack(size: 16);
        button.Label.ClipText = true;
        button.AddStyleClass(StyleNano.StyleClassCrtButton);
        button.OnPressed += _ => open(); parent.AddChild(button);
        CrtLobbyTheme.Apply(button, useCrtTypography: false);
        return button;
    }
}
