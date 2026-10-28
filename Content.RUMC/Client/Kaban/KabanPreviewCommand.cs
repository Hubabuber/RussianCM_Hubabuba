using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace Content.Client._RuCM.Kaban;

/// <summary>Disconnected visual QA with synthetic balances and a delegate that cannot send requests.</summary>
[AnyCommand]
public sealed class KabanPreviewCommand : LocalizedCommands
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private KabanWindow? _window;
    private CaptureControl? _capture;

    public override string Command => "kabanpreview";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var minimum = args.Contains("minimum");
        var pages = args.Where(argument => argument != "minimum").ToArray();
        if (args.Length > 2 || pages.Length > 1 || pages.Length == 1 && pages[0] is not
            ("capture" or "catalog" or "checkout" or "force-checkout" or "forcing" or "unavailable" or "pending" or "inventory" or "wallet" or "admin" or "priorities" or "priority-armed" or "off"))
        {
            shell.WriteError(Help);
            return;
        }
        if (_players.LocalSession != null)
        {
            shell.WriteError(Loc.GetString("cmu-kaban-preview-disconnected"));
            return;
        }

        _capture?.Dispose();
        _capture = null;
        _window?.Dispose();
        _window = null;
        var page = pages.FirstOrDefault() ?? "catalog";
        if (page == "off")
            return;

        shell.WriteLine(Loc.GetString("cmu-kaban-preview-local"));
        if (page == "capture")
        {
            _capture = new CaptureControl(this, _clyde, _resources, shell, minimum);
            _ui.RootControl.AddChild(_capture);
        }
        else
            Open(page, minimum);
    }

    private void Open(string page, bool minimum)
    {
        _window?.Dispose();
        // No networking, database, service access or production feature flags are involved.
        _window = new KabanWindow(_ => { })
        {
            Title = Loc.GetString("cmu-kaban-preview-title"),
        };
        _window.Update(page == "unavailable"
            ? new KabanState { Result = "cmu-kaban-storage-unavailable" }
            : CreateState());
        _window.ShowPreviewPage(page);
        if (minimum)
            _window.SetSize = new Vector2(820, 580);
        _window.OpenCentered();
    }

    private KabanState CreateState()
    {
        var contributor = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var manager = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var now = DateTime.UtcNow;
        var view = new KabanState
        {
            Viewer = contributor,
            Manager = true,
            PrioritiesEnabled = true,
            Lobby = true,
            ForcingEnabled = true,
            ForceOptions = new List<KabanForceOption> { new("ForceOnForce", Loc.GetString("cmu-gamemode-forceonforce-name"), "PreviewPlanet", "LV-624",
                new() { ["PreviewGovfor"] = "GOVFOR" }, new() { ["PreviewOpfor"] = "OPFOR" }) },
            Balance = 875,
            Earned = 1435,
            Accounts = new() { [contributor] = "PreviewContributor", [manager] = "PreviewManager" },
            Jobs = KabanJobCatalog.Jobs(_prototypes, "Insurgency")
                .ToDictionary(job => job.ID, KabanJobCatalog.DisplayName),
        };
        foreach (var (id, price) in new (string, int)[]
        {
            ("priority3", 60), ("priority5", 150), ("pack3", 165), ("pack5", 420),
            ("flex3", 100), ("flex5", 190), ("mode", 350), ("map", 250), ("govfor", 200),
            ("opfor", 200), ("full", 800), ("voice", 600), ("discord", 300), ("rename", 50),
        })
        {
            view.Products.Add(new(id, price, true, ""));
        }

        view.Items.Add(new(Guid.NewGuid(), "token", "flex3", now.AddDays(21).Ticks, "available", 3, true));
        view.Items.Add(new(Guid.NewGuid(), "token", "priority5", now.AddDays(12).Ticks, "reserved", 5,
            Jobs: view.Jobs.Keys.Take(1).ToArray()));
        view.Items.Add(new(contributor, "voice", Loc.GetString("cmu-kaban-product-voice"), 0, "active"));
        view.Items.Add(new(contributor, "discord", Loc.GetString("cmu-kaban-product-discord"), 0, "pending"));
        view.Items.Add(new(Guid.NewGuid(), "force", "full", 0, "queued",
            Detail: new KabanForceChoice("ForceOnForce", "PreviewPlanet", "PreviewGovfor", "PreviewOpfor").Encode(), Position: 2, OwnerName: "PreviewContributor"));
        view.History.Add(new(Guid.NewGuid(), now.AddHours(-1).Ticks, "purchase:flex3", -100));
        view.History.Add(new(Guid.NewGuid(), now.AddDays(-1).Ticks, "purchase:priority5", -150));
        view.History.Add(new(Guid.NewGuid(), now.AddDays(-2).Ticks,
            "award:development:" + Loc.GetString("cmu-kaban-category-development"), 300));
        view.Wallets.Add(new(contributor, view.Balance, view.Earned));
        view.Products = view.Products.OrderBy(product => product.Price).ToList();
        KabanWindow.QualifyJobNames(view);
        foreach (var duplicate in view.Jobs.GroupBy(job => job.Value, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            Logger.WarningS("kaban.preview", "Kaban catalogue duplicate display: " + duplicate.Key + " / " + string.Join(", ", duplicate.Select(job => job.Key)));
        return view;
    }

    private sealed class CaptureControl : Control
    {
        private readonly KabanPreviewCommand _owner;
        private readonly IClyde _clyde;
        private readonly IResourceManager _resources;
        private readonly IConsoleShell _shell;
        private readonly (string Page, bool Minimum)[] _scenarios;
        private int _index;
        private int _frames;
        private bool _saving;
        private bool _opened;

        public CaptureControl(KabanPreviewCommand owner, IClyde clyde, IResourceManager resources,
            IConsoleShell shell, bool minimumOnly)
        {
            _owner = owner;
            _clyde = clyde;
            _resources = resources;
            _shell = shell;
            MouseFilter = MouseFilterMode.Ignore;
            var sizes = minimumOnly ? new[] { true } : new[] { false, true };
            _scenarios = sizes.SelectMany(minimum => new[]
            {
                ("catalog", minimum), ("checkout", minimum), ("inventory", minimum),
                ("unavailable", minimum), ("pending", minimum),
                ("admin", minimum),
                ("priorities", minimum), ("priority-armed", minimum),
                ("forcing", minimum), ("force-checkout", minimum),
            }).ToArray();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            // Startup commands run before the main menu has finished replacing its UI tree.
            if (!_opened)
            {
                if (++_frames < 180) return;
                _opened = true;
                _frames = 0;
                _owner.Open(_scenarios[0].Page, _scenarios[0].Minimum);
                return;
            }
            if (_saving || ++_frames < 60)
                return;
            if (_index >= _scenarios.Length)
            {
                _shell.ExecuteCommand("quit");
                return;
            }

            _saving = true;
            var (page, minimum) = _scenarios[_index];
            var name = page + (minimum ? "-minimum" : "-normal");
            var window = _owner._window!;
            var position = window.GlobalPixelPosition;
            var size = window.PixelSize;
            var viewport = _clyde.ScreenSize;
            var bounds = $"KabanPreview/{name}: position={position}, size={size}, viewport={viewport}";
            if (position.X < 0 || position.Y < 0 || position.X + size.X > viewport.X || position.Y + size.Y > viewport.Y)
                _shell.WriteError(bounds + " OUTSIDE VIEWPORT");
            else
                _shell.WriteLine(bounds + " OK");
            foreach (var card in Descendants(window).Where(control => control.Name?.StartsWith("Card-", StringComparison.Ordinal) == true))
            {
                foreach (var label in Descendants(card).OfType<RichTextLabel>())
                    if (label.DesiredSize.X > label.Size.X + 1 || label.GlobalPixelPosition.X + label.PixelSize.X > card.GlobalPixelPosition.X + card.PixelSize.X + 1)
                        _shell.WriteError("KabanPreview text outside card: " + card.Name);
            }

            _clyde.Screenshot(ScreenshotType.Final, screenshot =>
            {
                var directory = new ResPath("/KabanPreview");
                _resources.UserData.CreateDir(directory);
                using var file = _resources.UserData.Open(directory / (name + ".png"),
                    FileMode.Create, FileAccess.Write, FileShare.None);
                screenshot.SaveAsPng(file);
                _shell.WriteLine("KabanPreview/" + name + ".png");
                _index++;
                _frames = 0;
                _saving = false;
                if (_index < _scenarios.Length)
                    _owner.Open(_scenarios[_index].Page, _scenarios[_index].Minimum);
            });
        }

        private static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
        {
            foreach (var child in parent.Children)
            {
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }
    }
}

public sealed partial class KabanWindow
{
    /// <summary>Used only by the disconnected synthetic preview; never changes server state.</summary>
    internal void ShowPreviewPage(string page)
    {
        _pending = null;
        _checkout = null;
        _section = page is "catalog" or "checkout" or "force-checkout" or "pending" or "unavailable" ? "shop"
            : page == "priority-armed" ? "priorities" : page;
        _category = "all";
        if (page == "priorities") _view.Items.RemoveAll(item => item.Kind == "token" && item.Status == "reserved");
        if (page is "priorities" or "priority-armed")
            _token = _view.Items.First(item => item.Kind == "token" && item.Status == (page == "priorities" ? "available" : "reserved")).Id;
        if (page == "priorities") _jobChoices[_token] = new System.Collections.Generic.HashSet<string>(_view.Jobs.Keys.Take(1));
        Render();
        if (page == "checkout")
            ConfirmPurchase(_view.Products.First(product => product.Id == "voice"));
        else if (page == "force-checkout")
            ConfirmPurchase(_view.Products.First(product => product.Id == "full"));
        else if (page == "pending")
            Submit(new KabanRequest { Action = KabanAction.Purchase, Product = "priority3", Amount = 60, Confirmed = true });
    }
}
