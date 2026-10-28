using System.IO;
using System.Linq;
using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace Content.Client._RuCM.Qualifications;

/// <summary>Local, disconnected visual QA. Synthetic screens, no network actions or database.</summary>
[AnyCommand]
public sealed class QualificationEntryPreviewCommand : LocalizedCommands
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private Robust.Client.Player.IPlayerManager _players = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceManager _resources = default!;
    public override string Command => "qualificationentrypreview";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0) { shell.WriteError(Help); return; }
        if (_players.LocalSession != null) { shell.WriteError(Robust.Shared.Localization.Loc.GetString("rucm-qualifications-preview-disconnected-only")); return; }
        // CMU14: preview the personalization action using the same composition helper, without network actions.
        var actions = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, MinWidth = 325 };
        var record = QualificationEntrySystem.AttachPersonalization(actions, () => { });
        foreach (var button in QualificationEntrySystem.Descendants(actions).OfType<BaseButton>()) button.Disabled = button != record;
        var stage = new Robust.Client.UserInterface.CustomControls.DefaultWindow
        { Title = Robust.Shared.Localization.Loc.GetString("rucm-qualifications-entry-preview-title"), SetSize = new(480, 360) };
        stage.Contents.AddChild(actions); stage.OpenCentered();
        _ui.RootControl.AddChild(new Capture(_ui, _clyde, _resources, shell, stage));
    }
    private sealed class Capture : Control
    {
        private readonly IUserInterfaceManager _ui;
        private readonly IClyde _clyde;
        private readonly IResourceManager _resources;
        private readonly IConsoleShell _shell;
        private readonly Robust.Client.UserInterface.CustomControls.DefaultWindow _stage;
        private int _phase;
        private int _frames;
        private bool _saving;
        public Capture(IUserInterfaceManager ui, IClyde clyde, IResourceManager resources, IConsoleShell shell, Robust.Client.UserInterface.CustomControls.DefaultWindow stage)
        { _stage = stage; _ui = ui; _clyde = clyde; _resources = resources; _shell = shell; MouseFilter = MouseFilterMode.Ignore; }
        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (_saving) return;
            _frames++;
            if (_phase == 0 && _frames == 10) _stage.RecenterWindow(new(0.5f, 0.5f));
            if (_frames < 60) return;
            if (_phase > 1) { _shell.ExecuteCommand("quit"); return; }
            _saving = true;
            var name = _phase == 0 ? "entry-personalization" : "entry-escape";
            _clyde.Screenshot(ScreenshotType.Final, screenshot =>
            {
                var directory = new ResPath("/QualificationPreview"); _resources.UserData.CreateDir(directory);
                using var file = _resources.UserData.Open(directory / (name + ".png"), FileMode.Create, FileAccess.Write, FileShare.None);
                screenshot.SaveAsPng(file); _shell.WriteLine("QualificationPreview/" + name + ".png");
                _phase++; _frames = 0; _saving = false;
                if (_phase == 1)
                {
                    _stage.Dispose();
                    var escape = new EscapeMenu();
                    QualificationEntrySystem.AttachEscape(escape, true, () => { });
                    escape.OpenCentered();
                }
            });
        }
    }
}
