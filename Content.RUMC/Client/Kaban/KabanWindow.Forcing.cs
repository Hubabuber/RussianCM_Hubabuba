using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RuCM.Kaban;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client._RuCM.Kaban;

public sealed partial class KabanWindow
{
    private KabanForceChoice _forceDraft = new();

    private void ForceSelect(BoxContainer form, string field, IEnumerable<KeyValuePair<string, string>> values,
        string current, Action<string> changed)
    {
        Text(form, L("force-field-" + field), Ink);
        var items = values.DistinctBy(p => p.Key).OrderBy(p => p.Value).ToArray();
        var select = new OptionButton { HorizontalExpand = true, MinHeight = 36, MaxWidth = 540,
            RectClipContent = true, Filterable = true, Name = "ForceSelect-" + field };
        // OptionButton's built-in label measures its entire text unless explicitly clipped.
        foreach (var label in select.Children.SelectMany(c => c.Children).OfType<Label>())
            label.ClipText = true;
        select.AddItem(L("force-select"), 0);
        for (var i = 0; i < items.Length; i++)
        {
            select.AddItem(items[i].Value, i + 1);
            if (items[i].Key == current)
                select.SelectId(i + 1);
        }
        select.Disabled = items.Length == 0;
        select.OnItemSelected += args =>
        {
            changed(args.Id == 0 ? "" : items[args.Id - 1].Key);
            _purchaseText = _forceDraft.Encode();
            _purchaseConfirmed = false;
            Render();
        };
        form.AddChild(select);
    }

    private void RenderForceFields(string product)
    {
        var form = Card(_page, L("force-selection"));
        var options = _view.ForceOptions.AsEnumerable();
        if (product == "full")
            options = options.Where(o => o.Govfor.Keys.Any(g => o.Opfor.Keys.Any(p => p != g)));
        if (product is "mode" or "full")
        {
            ForceSelect(form, "mode", options.Select(o => KeyValuePair.Create(o.Mode, o.ModeName)), _forceDraft.Mode,
                id => _forceDraft = new(Mode: id));
            options = options.Where(o => o.Mode == _forceDraft.Mode);
        }
        if (product is "map" or "full")
        {
            ForceSelect(form, "map", options.Select(o => KeyValuePair.Create(o.Planet, o.PlanetName)), _forceDraft.Planet,
                id => _forceDraft = _forceDraft with { Planet = id, Govfor = "", Opfor = "" });
            options = options.Where(o => o.Planet == _forceDraft.Planet);
        }
        if (product is "govfor" or "full")
            ForceSelect(form, "govfor", options.SelectMany(o => o.Govfor).Where(p => options.Any(o => new KabanForceChoice(Govfor: p.Key).Matches(o))), _forceDraft.Govfor,
                id => _forceDraft = _forceDraft with { Govfor = id, Opfor = "" });
        if (product is "opfor" or "full")
            ForceSelect(form, "opfor", options.SelectMany(o => o.Opfor).Where(p => p.Key != _forceDraft.Govfor
                && options.Any(o => new KabanForceChoice(Opfor: p.Key).Matches(o))), _forceDraft.Opfor,
                id => _forceDraft = _forceDraft with { Opfor = id });
        Text(form, ValidPurchaseFields(product) ? L("force-compatible") : L("force-selection-help"), Muted);
        Text(Card(_page, L("store-conditions")), L("force-rules"));
    }

    private void RenderForcing()
    {
        Text(Card(_page), L("force-rules"));
        if (!_view.ForcingEnabled)
            Text(_page, L("forcing-disabled"), Warning);
        Button(_page, L("force-shop"), () => { _section = "shop"; _category = "round"; Render(); });
        var requests = _view.Items.Where(i => i.Kind == "force").ToArray();
        if (requests.Length == 0)
            Text(_page, L("account-empty-requests"));
        foreach (var item in requests)
        {
            var card = Card(_page, L("product-" + item.Description));
            if (item.OwnerName.Length > 0)
                Text(card, Loc.GetString("cmu-kaban-force-owner", ("owner", item.OwnerName)));
            if (KabanForceChoice.Decode(item.Detail) is { } choice)
            {
                var names = _view.ForceOptions;
                if (choice.Mode != "") Text(card, L("force-field-mode") + ": " + (names.FirstOrDefault(o => o.Mode == choice.Mode)?.ModeName ?? choice.Mode));
                if (choice.Planet != "") Text(card, L("force-field-map") + ": " + (names.FirstOrDefault(o => o.Planet == choice.Planet)?.PlanetName ?? choice.Planet));
                if (choice.Govfor != "") Text(card, "GOVFOR: " + (names.FirstOrDefault(o => o.Govfor.ContainsKey(choice.Govfor))?.Govfor[choice.Govfor] ?? choice.Govfor));
                if (choice.Opfor != "") Text(card, "OPFOR: " + (names.FirstOrDefault(o => o.Opfor.ContainsKey(choice.Opfor))?.Opfor[choice.Opfor] ?? choice.Opfor));
            }
            Text(card, L("status-" + item.Status));
            if (item.Position > 0)
                Text(card, Loc.GetString("cmu-kaban-force-position", ("position", item.Position)));
            Button(card, L("cancel"), () => Submit(new KabanRequest { Action = KabanAction.CancelRequest, Item = item.Id }), item.Status != "queued");
        }
    }
}
