using System;
using System.Linq;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Corvax.TTS;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._RuCM.Kaban;

public sealed partial class KabanWindow
{
    private string _category = "all";
    private string _search = "";
    private KabanCardGrid? _grid;
    private KabanProductView? _checkout;
    private string _purchaseText = "";
    private string _purchaseCategory = "";
    private bool _purchaseConfirmed;

    private static string Category(string id) => id switch
    {
        "voice" or "discord" or "rename" => "personal",
        "mode" or "map" or "govfor" or "opfor" or "full" => "round",
        _ => "priorities",
    };

    private static Color Accent(string id) => Category(id) switch
    {
        "personal" => Color.FromHex("#B6A5EC"),
        "round" => Color.FromHex("#85C9C9"),
        _ => Color.FromHex("#91B9F0"),
    };

    private static string Badge(string id) => id switch
    {
        "priority3" or "flex3" => "×3",
        "priority5" or "flex5" => "×5",
        "pack3" => "3 × ×3",
        "pack5" => "3 × ×5",
        "voice" or "discord" => L("permanent"),
        "rename" => L("store-customization"),
        _ => L("store-round"),
    };

    private void ResizeCatalog()
    {
        if (_grid != null)
            _grid.InvalidateMeasure();
    }

    private void RenderShop()
    {
        var hero = Card(_page, L("store-hero"), L("store-hero-help"));
        var tabs = Row();
        tabs.SeparationOverride = 6;
        foreach (var category in new[] { "all", "priorities", "personal", "round" })
        {
            var button = (StoreButton) Button(tabs, L("store-category-" + category), () =>
            {
                _category = category;
                Render();
                _scroll.SetScrollValue(default);
            });
            button.Selected = category == _category;
            button.Label.FontOverride = UiFont(12);
            button.Name = "StoreCategory-" + category;
        }
        hero.AddChild(tabs);
        var search = Input(hero, L("store-search"));
        search.Name = "ShopSearch";
        search.Text = _search;
        var results = Column();
        _page.AddChild(results);
        search.OnTextChanged += _ => { _search = search.Text; RenderCatalog(results); };
        RenderCatalog(results);
    }

    private void RenderCatalog(BoxContainer results)
    {
        results.RemoveAllChildren();
        var products = _view.Products.Where(p => (_category == "all" || Category(p.Id) == _category)
            && (L("product-" + p.Id).Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase)
                || L("description-" + p.Id).Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase))).ToArray();
        Text(results, Loc.GetString("cmu-kaban-store-results", ("count", products.Length)), Muted);
        _grid = new KabanCardGrid
        {
            HorizontalExpand = true,
            Name = "ProductGrid",
        };
        results.AddChild(_grid);
        ResizeCatalog();
        if (products.Length == 0)
        {
            Card(results, L("store-no-results"), L("store-search-help"));
            return;
        }
        foreach (var product in products)
            ProductCard(product);
    }

    private void ProductCard(KabanProductView product)
    {
        var accent = Accent(product.Id);
        var panel = Panel(Surface, Edge);
        panel.RectClipContent = true;
        panel.MinHeight = 242;
        panel.Name = "Card-" + product.Id;
        var card = Column();
        panel.AddChild(card);
        _grid!.AddChild(panel);
        var top = Row();
        var tile = Panel(accent.WithAlpha(0.12f));
        tile.HorizontalExpand = false;
        tile.MinSize = new(64, 64);
        tile.AddChild(Icon(product.Id, 40, product.Id is "voice" or "map" ? null : accent));
        top.AddChild(tile);
        var titles = Column();
        titles.SeparationOverride = 4;
        Text(titles, Badge(product.Id), accent);
        Heading(titles, L("product-" + product.Id));
        top.AddChild(titles);
        card.AddChild(top);
        Text(card, L("store-summary-" + product.Id));
        card.AddChild(new Control { VerticalExpand = true });
        var status = !product.Available ? L("store-unavailable")
            : _view.Balance < product.Price ? L("insufficient") : L("store-available");
        Text(card, status, product.Available ? Muted : Warning);
        var bottom = Row();
        bottom.AddChild(Currency(product.Price));
        var button = Button(bottom, L("store-details"), () => ConfirmPurchase(product));
        button.Name = "Product-" + product.Id;
        card.AddChild(bottom);
        if (product.Blocker.Length > 0)
            button.ToolTip = Loc.GetString(product.Blocker);
    }

    private void ConfirmPurchase(KabanProductView product)
    {
        _checkout = product;
        _purchaseText = "";
        _purchaseCategory = "";
        _purchaseConfirmed = false;
        _forceDraft = new();
        Render();
        _scroll.SetScrollValue(default);
    }

    private bool ValidPurchaseFields(string id)
    {
        if (Category(id) == "round")
            return _forceDraft.ValidFor(id) && _view.ForceOptions.Any(_forceDraft.Matches);
        if (id == "voice")
            return CustomTTSVoice.IsValidSpeakerName(_purchaseCategory) && _purchaseText.Trim().Length >= 20;
        if (id is "discord" or "rename")
        {
            var name = _purchaseText.Trim();
            return name.Length is >= 2 and <= 32
                && name.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')
                && _purchaseCategory.Length == 6 && _purchaseCategory.All(Uri.IsHexDigit);
        }
        return true;
    }

    private void RenderCheckout()
    {
        var product = _checkout!;
        Button(_page, L("store-back"), () => { _checkout = null; Render(); }).Name = "CheckoutBack";
        var details = Card(_page);
        var header = Row();
        var icon = Panel(Accent(product.Id).WithAlpha(0.12f));
        icon.HorizontalExpand = false;
        icon.AddChild(Icon(product.Id, 72, product.Id is "voice" or "map" ? null : Accent(product.Id)));
        header.AddChild(icon);
        var titles = Column();
        Text(titles, L("store-category-" + Category(product.Id)), Accent(product.Id));
        Heading(titles, L("product-" + product.Id));
        Text(titles, Badge(product.Id));
        header.AddChild(titles);
        details.AddChild(header);
        Text(details, L("description-" + product.Id), Ink);
        if (Category(product.Id) == "priorities")
            Text(Card(_page, L("store-conditions")), L("priority-rules"));
        if (product.Blocker.Length > 0)
            Text(Card(_page, L("store-unavailable"), accent: Warning), Loc.GetString(product.Blocker), Warning);
        if (_view.Balance < product.Price)
            Text(Card(_page), Loc.GetString("cmu-kaban-store-shortfall", ("amount", product.Price - _view.Balance)), Warning);

        if (Category(product.Id) == "round")
            RenderForceFields(product.Id);

        var total = Card(_page, L("store-order"));
        var cost = Row();
        Text(cost, L("store-cost"));
        cost.AddChild(Currency(product.Price, 22));
        total.AddChild(cost);
        var remaining = Row();
        Text(remaining, L("store-remaining"));
        remaining.AddChild(Currency(_view.Balance - product.Price, 18, _view.Balance < product.Price ? Warning : Ink));
        total.AddChild(remaining);

        // Drafts survive authoritative refresh; the server still validates every field and the quoted price.
        Button? confirm = null;
        void RefreshConfirm()
        {
            if (confirm != null)
                confirm.Disabled = !_purchaseConfirmed || !product.Available || _view.Balance < product.Price || !ValidPurchaseFields(product.Id);
        }
        if (product.Id is "voice" or "discord" or "rename")
        {
            var form = Card(_page, L("store-customization"));
            var textKey = product.Id == "voice" ? "consent" : "role-name";
            var categoryKey = product.Id == "voice" ? "speaker-id" : "role-color";
            Text(form, L(categoryKey), Ink);
            var category = Input(form, L(categoryKey));
            category.Name = "PurchaseCategory";
            category.Text = _purchaseCategory;
            category.OnTextChanged += _ => { _purchaseCategory = category.Text; RefreshConfirm(); };
            Text(form, L(textKey), Ink);
            var text = Input(form, L(textKey));
            text.Name = "PurchaseText";
            text.Text = _purchaseText;
            text.OnTextChanged += _ => { _purchaseText = text.Text; RefreshConfirm(); };
        }

        // The consequential action stays below the scrolling details, at both supported window sizes.
        var footer = Card(_actions);
        var consent = Row();
        var check = new CheckBox { Pressed = _purchaseConfirmed, Name = "PurchaseConfirm", ToolTip = L("confirm-purchase") };
        consent.AddChild(check);
        Text(consent, L("confirm-purchase"), Ink);
        footer.AddChild(consent);
        confirm = Button(footer, Loc.GetString("cmu-kaban-store-purchase", ("price", product.Price)), () => Submit(new KabanRequest
        {
            Action = KabanAction.Purchase, Product = product.Id, Amount = product.Price,
            Text = _purchaseText, Category = _purchaseCategory, Confirmed = _purchaseConfirmed,
        }), primary: true);
        confirm.Name = "PurchaseSubmit";
        check.OnToggled += e => { _purchaseConfirmed = e.Pressed; RefreshConfirm(); };
        RefreshConfirm();
    }
}
