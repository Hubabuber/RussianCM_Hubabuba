using System;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Kaban;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._RuCM.Kaban;

/// <summary>Account storefront. Prices, availability and purchase results always come from the server.</summary>
public sealed partial class KabanWindow : DefaultWindow
{
    private static readonly Color Background = Color.FromHex("#10151D");
    private static readonly Color Surface = Color.FromHex("#19222E");
    private static readonly Color Edge = Color.FromHex("#344254");
    private static readonly Color Ink = Color.FromHex("#EAF0F7");
    private static readonly Color Muted = Color.FromHex("#AAB9CB");
    private static readonly Color Gold = Color.FromHex("#F1C46B");
    private static readonly Color Warning = Color.FromHex("#E9A287");
    private readonly Action<KabanRequest> _send;
    private readonly BoxContainer _page = Column();
    private readonly BoxContainer _nav = Column();
    private readonly BoxContainer _balance = Row();
    private readonly BoxContainer _actions = Column();
    private readonly PanelContainer _feedbackPanel = Panel(Surface);
    private readonly RichTextLabel _feedback = new() { HorizontalExpand = true };
    private readonly ScrollContainer _scroll = new()
    {
        HorizontalExpand = true, VerticalExpand = true, HScrollEnabled = false, Name = "StoreScroll",
    };
    private KabanState _view = new();
    private string _section = "shop";
    private Guid _token;
    private KabanRequest? _pending;

    private static string L(string key) => Loc.GetString("cmu-kaban-" + key);
    private static BoxContainer Column() => new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 10, HorizontalExpand = true,
    };
    private static BoxContainer Row() => new() { SeparationOverride = 10, HorizontalExpand = true };
    private static Font UiFont(int size = 14, bool bold = false)
        => IoCManager.Resolve<IResourceCache>().NotoStack(size: size, variation: bold ? "Bold" : "Regular");

    public KabanWindow(Action<KabanRequest> send)
    {
        _send = send;
        Title = L("title");
        MinSize = new(820, 580);
        var viewport = IoCManager.Resolve<IUserInterfaceManager>().WindowRoot.Size;
        SetSize = new(Math.Clamp(viewport.X - 40, 820, 1100), Math.Clamp(viewport.Y - 40, 580, 760));
        var screen = Panel(Background);
        screen.VerticalExpand = true;
        var root = Column();
        root.Margin = new Thickness(14);
        root.VerticalExpand = true;
        screen.AddChild(root);
        Contents.AddChild(screen);

        var header = Row();
        header.AddChild(Icon("wallet", 48));
        var brand = Column();
        brand.SeparationOverride = 0;
        Heading(brand, L("store-title"));
        Text(brand, L("store-subtitle"), Muted);
        header.AddChild(brand);
        _balance.HorizontalExpand = false;
        _balance.VerticalAlignment = VAlignment.Center;
        header.AddChild(_balance);
        root.AddChild(header);
        root.AddChild(Divider());
        _feedbackPanel.AddChild(_feedback);
        root.AddChild(_feedbackPanel);

        var body = Row();
        body.SeparationOverride = 16;
        body.VerticalExpand = true;
        var navScroll = new ScrollContainer { HScrollEnabled = false, MinWidth = 174, MaxWidth = 174 };
        navScroll.AddChild(_nav);
        body.AddChild(navScroll);
        var workspace = Column();
        workspace.VerticalExpand = true;
        _page.Margin = new Thickness(0, 0, 10, 0);
        _scroll.AddChild(_page);
        _scroll.OnResized += ResizeCatalog;
        workspace.AddChild(_scroll);
        workspace.AddChild(_actions);
        body.AddChild(workspace);
        root.AddChild(body);
        Render();
    }

    private static PanelContainer Panel(Color fill, Color? border = null) => new()
    {
        HorizontalExpand = true,
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = fill, BorderColor = border ?? Edge,
            BorderThickness = new Thickness(border == null ? 0 : 1),
            ContentMarginLeftOverride = 12, ContentMarginRightOverride = 12,
            ContentMarginTopOverride = 10, ContentMarginBottomOverride = 10,
        },
    };

    private static PanelContainer Divider() => new()
    {
        MinHeight = 1, PanelOverride = new StyleBoxFlat { BackgroundColor = Edge },
    };

    private static RichTextLabel Text(BoxContainer box, string value, Color? color = null)
    {
        var label = new RichTextLabel { HorizontalExpand = true, LineHeightScale = 1.15f };
        label.SetMessage(FormattedMessage.FromUnformatted(value), color ?? Muted);
        box.AddChild(label);
        return label;
    }

    private static void Heading(BoxContainer box, string title)
    {
        var label = Text(box, title, Ink);
        label.SetMessage(FormattedMessage.FromMarkupPermissive("[font size=18][bold]"
            + FormattedMessage.EscapeText(title) + "[/bold][/font]"), Ink);
    }

    private static BoxContainer Card(BoxContainer parent, string title = "", string help = "", Color? accent = null)
    {
        var panel = Panel(Surface, accent ?? Edge);
        var body = Column();
        panel.AddChild(body);
        parent.AddChild(panel);
        if (title.Length > 0) Heading(body, title);
        if (help.Length > 0) Text(body, help);
        return body;
    }

    private static Button Button(BoxContainer box, string text, Action action, bool disabled = false, bool primary = false)
    {
        var button = new StoreButton(primary) { Text = text, HorizontalExpand = true, MinHeight = 38, Disabled = disabled };
        button.Label.FontOverride = UiFont();
        button.Label.HorizontalExpand = true;
        button.Label.Align = Label.AlignMode.Center;
        button.Label.ClipText = true;
        button.ToolTip = text;
        button.OnPressed += _ => action();
        box.AddChild(button);
        return button;
    }

    private static LineEdit Input(BoxContainer box, string placeholder)
    {
        var input = new LineEdit { PlaceHolder = placeholder, HorizontalExpand = true, MinHeight = 34, ToolTip = placeholder };
        box.AddChild(input);
        return input;
    }

    private static TextureRect Icon(string name, int size = 32, Color? tint = null)
    {
        var path = name switch
        {
            "wallet" or "shop" => "/Textures/_RuCM/Kaban/coin.png",
            "voice" => "/Textures/Interface/Actions/voice-mask.rsi/icon.png",
            "map" or "force" or "forcing" => "/Textures/Interface/Actions/pai-map.rsi/icon.png",
            "token" or "priorities" or "priority3" or "priority5" => "/Textures/Interface/students-cap.svg.192dpi.png",
            "inventory" or "pack3" or "pack5" => "/Textures/Interface/inventory.svg.192dpi.png",
            "history" => "/Textures/Interface/VerbIcons/clock.svg.192dpi.png",
            "awards" => "/Textures/Interface/examine-star.png",
            "admin" => "/Textures/CMU14/Interface/Icons/gear.svg.192dpi.png",
            "flex3" or "flex5" or "refresh" => "/Textures/CMU14/Interface/Icons/arrows-rotate.svg.192dpi.png",
            "discord" or "rename" => "/Textures/CMU14/Interface/Icons/link.svg.192dpi.png",
            "govfor" or "opfor" => "/Textures/Interface/character.svg.192dpi.png",
            _ => "/Textures/CMU14/Interface/Icons/server.svg.192dpi.png",
        };
        return new TextureRect
        {
            Texture = IoCManager.Resolve<IResourceCache>().GetTexture(path),
            MinSize = new(size, size), MaxSize = new(size, size), CanShrink = true,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            VerticalAlignment = VAlignment.Center, HorizontalAlignment = HAlignment.Center,
            ModulateSelfOverride = tint ?? Color.White,
        };
    }

    private static BoxContainer Currency(long amount, int size = 18, Color? color = null)
    {
        var row = Row();
        row.SeparationOverride = 6;
        row.HorizontalExpand = false;
        row.AddChild(Icon("wallet", size + 6));
        row.AddChild(new Label
        {
            Text = amount.ToString("N0"), FontOverride = UiFont(size, true), FontColorOverride = color ?? Gold,
            VerticalAlignment = VAlignment.Center,
        });
        return row;
    }

    public void Update(KabanState view)
    {
        QualifyJobNames(view);
        _view = view;
        if (_pending != null && view.Response == _pending.Operation && view.Result != "cmu-kaban-storage-unavailable")
        {
            _pending = null;
            _checkout = null;
        }
        if (_checkout != null)
        {
            var current = view.Products.Find(p => p.Id == _checkout.Id);
            if (current == null || current.Price != _checkout.Price || !current.Available)
                _purchaseConfirmed = false;
            _checkout = current;
        }
        _feedbackPanel.Visible = view.Result.Length > 0;
        _feedback.SetMessage(FormattedMessage.FromUnformatted(view.Result.Length > 0 ? Loc.GetString(view.Result) : ""),
            view.Result == "cmu-kaban-ok" ? Gold : Warning);
        Render();
    }

    private void Submit(KabanRequest request)
    {
        if (_pending != null) return;
        _pending = request;
        _send(request);
        Render();
    }

    private void Navigate(string section)
    {
        _section = section;
        _checkout = null;
        Render();
        _scroll.SetScrollValue(default);
    }

    private void Render()
    {
        _balance.RemoveAllChildren();
        if (_view.Products.Count > 0) _balance.AddChild(Currency(_view.Balance, 23));
        else Text(_balance, L("store-loading"));
        _nav.RemoveAllChildren();
        Text(_nav, L("store-navigation"), Muted);
        foreach (var section in new[] { "shop", "inventory", "wallet", "priorities", "forcing", "history", "admin" })
        {
            if (section == "admin" && !_view.Manager) continue;
            var row = Row();
            row.SeparationOverride = 7;
            row.AddChild(Icon(section, 20, section == "shop" || section == "wallet" ? null : Muted));
            var button = (StoreButton) Button(row, L("nav-" + section), () => Navigate(section));
            button.Selected = section == _section;
            button.Name = "Nav-" + section;
            button.Label.FontOverride = UiFont(12);
            button.Label.Align = Label.AlignMode.Left;
            _nav.AddChild(row);
        }
        _nav.AddChild(Divider());
        Button(_nav, L("refresh"), () => _send(new KabanRequest { Action = KabanAction.Refresh }));
        Text(_nav, L("store-nav-help"));
        _page.RemoveAllChildren();
        _actions.RemoveAllChildren();
        _grid = null;
        if (_pending != null)
        {
            var pending = Card(_page, L("store-processing"), L("pending"), Gold);
            pending.AddChild(Icon("history", 56, Gold));
            Text(pending, L("retry-help"));
            // Retry the captured operation, including its original price and explicit confirmation.
            var request = _pending;
            Button(_actions, L("retry"), () => _send(request), primary: true).Name = "PurchaseRetry";
            return;
        }
        if (_view.Products.Count == 0)
        {
            Card(_page, L("store-loading"), L("unavailable"));
            return;
        }
        if (_section == "shop")
        {
            if (_checkout != null) RenderCheckout();
            else RenderShop();
        }
        else RenderAccount();
    }

    /// <summary>Local palette with distinct hover, selected, disabled and purchase states.</summary>
    private sealed class StoreButton : Button
    {
        private readonly bool _primary;
        private bool _selected;
        public bool Selected { set { _selected = value; Apply(); } }

        public StoreButton(bool primary)
        {
            _primary = primary;
            // The default Button stylesheet multiplies its surface color; this control owns its palette.
            ModulateSelfOverride = Color.White;
            Apply();
        }
        protected override void DrawModeChanged() { base.DrawModeChanged(); Apply(); }
        private void Apply()
        {
            if (Label == null) return;
            var disabled = DrawMode == DrawModeEnum.Disabled;
            var active = _primary || _selected;
            var hover = DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed;
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = disabled ? Background : active
                    ? hover ? Color.FromHex("#F9D994") : Gold
                    : hover ? Color.FromHex("#334359") : Surface,
                BorderColor = active && !disabled ? Gold : Edge,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10, ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 5, ContentMarginBottomOverride = 5,
            };
            Label.FontColorOverride = disabled ? Muted.WithAlpha(0.6f) : active ? Background : Ink;
        }
    }
}
