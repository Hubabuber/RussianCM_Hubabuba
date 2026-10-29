using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RuCM.Kaban;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client._RuCM.Kaban;

public sealed partial class KabanWindow
{
    private readonly Dictionary<string, string> _accountDrafts = new();
    private readonly Dictionary<string, bool> _accountChecks = new();
    private int _settingsProduct;

    private static string Date(long ticks)
        => ticks == 0 ? L("permanent") : new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("g");

    private string Account(Guid id) => _view.Accounts.GetValueOrDefault(id, id.ToString());

    private LineEdit DraftInput(BoxContainer box, string key, string caption, string initial = "")
    {
        Text(box, caption);
        var input = Input(box, caption);
        input.Text = _accountDrafts.GetValueOrDefault(key, initial);
        input.OnTextChanged += _ => _accountDrafts[key] = input.Text;
        return input;
    }

    private CheckBox DraftCheck(BoxContainer box, string key, string caption)
    {
        var check = new CheckBox
        {
            Text = caption,
            Pressed = _accountChecks.GetValueOrDefault(key),
            HorizontalExpand = true,
            MinHeight = 32,
            ToolTip = caption,
        };
        check.Label.HorizontalExpand = true;
        check.Label.ClipText = true;
        check.OnToggled += e => _accountChecks[key] = e.Pressed;
        box.AddChild(check);
        return check;
    }

    private LineEdit AccountInput(BoxContainer box, string scope)
    {
        Text(box, L("account-owner"));
        var accounts = _view.Accounts.OrderBy(a => a.Value).ToArray();
        var select = new OptionButton { HorizontalExpand = true, MinHeight = 34 };
        select.AddItem(L("select-account"), 0);
        for (var i = 0; i < accounts.Length; i++)
            select.AddItem(accounts[i].Value, i + 1);
        box.AddChild(select);
        var input = Input(box, L("account-id"));
        input.Text = _accountDrafts.GetValueOrDefault(scope + "/target", "");
        input.OnTextChanged += _ => _accountDrafts[scope + "/target"] = input.Text;
        select.OnItemSelected += e =>
        {
            select.SelectId(e.Id);
            if (e.Id > 0)
                input.Text = accounts[e.Id - 1].Key.ToString();
        };
        return input;
    }

    private string ItemText(KabanItemView item)
        => item.Kind != "token" ? item.Description : (item.Flexible ? L("flexible") + " " : "")
            + $"×{item.Multiplier}"
            + (item.Jobs is { Length: > 0 }
                ? " · " + string.Join(", ", item.Jobs.Select(j => _view.Jobs.GetValueOrDefault(j, j)))
                : "");

    private static string HistoryText(string reason)
    {
        if (reason.StartsWith("manager-adjustment:", StringComparison.Ordinal))
        {
            var note = reason["manager-adjustment:".Length..];
            return L("history-manager-adjustment") + (note.Length > 0 ? ": " + note : "");
        }
        if (reason is "voice-special-grant" or "force-refund" or "privilege-refund")
            return L("history-" + reason);
        if (reason.StartsWith("purchase:", StringComparison.Ordinal))
            return L("product-" + reason[9..]);
        if (reason.StartsWith("award:", StringComparison.Ordinal))
        {
            var parts = reason.Split(':', 3);
            if (parts.Length == 3)
                return L("category-" + parts[1]) + ": " + parts[2];
        }
        return reason;
    }

    private void RenderAccount()
    {
        if (_section != "priorities") Heading(_page, L(_section));
        switch (_section)
        {
            case "wallet":
                var wallet = Card(_page, L("account-current-balance"));
                wallet.AddChild(Currency(_view.Balance, 32));
                Text(wallet, L("wallet-rules"));
                var earned = Card(_page, L("account-earned"));
                earned.AddChild(Currency(_view.Earned, 24));
                var walletActions = Row();
                _page.AddChild(walletActions);
                Button(walletActions, L("shop"), () => { _section = "shop"; Render(); }, primary: true);
                if (_view.Manager)
                    Button(walletActions, L("adjust-title"), () => Navigate("admin"));
                break;
            case "inventory":
                // Management snapshots include other accounts' privilege requests.
                var owned = _view.Items.Where(i => i.Kind == "token"
                    || i.Kind is "voice" or "discord" && i.Id == _view.Viewer).ToArray();
                if (owned.Length == 0)
                {
                    Text(Card(_page), L("account-empty-inventory"));
                    Button(_page, L("shop"), () => { _section = "shop"; Render(); }, primary: true);
                }
                foreach (var item in owned)
                {
                    var card = Card(_page, L("kind-" + item.Kind));
                    var line = Row();
                    line.AddChild(Icon(item.Kind));
                    Text(line, ItemText(item));
                    card.AddChild(line);
                    Text(card, L("status-" + item.Status));
                    Text(card, item.Expires == 0 ? L("permanent")
                        : Loc.GetString("cmu-kaban-account-token-expires", ("date", Date(item.Expires))));
                    if (item.Kind == "token")
                        Button(card, L("manage-token"), () => { _token = item.Id; _section = "priorities"; Render(); });
                }
                break;
            case "priorities":
                Priorities();
                break;
            case "forcing":
                RenderForcing();
                break;
            case "history":
                Text(_page, L("recent-history"));
                if (_view.History.Count == 0)
                    Text(Card(_page), L("account-empty-history"));
                foreach (var entry in _view.History)
                {
                    var card = Card(_page, HistoryText(entry.Description));
                    var line = Row();
                    line.AddChild(Icon("history", 24));
                    Text(line, Date(entry.At));
                    line.AddChild(Currency(entry.Amount, 20));
                    card.AddChild(line);
                    Text(card, entry.Id.ToString());
                }
                break;
            case "admin":
                Admin();
                break;
        }
    }

    private void Admin()
    {
        Text(Card(_page), L("admin-rules"));
        AdjustBalance();
        var accounts = Card(_page, L("wallet"));
        foreach (var wallet in _view.Wallets)
            Text(accounts, Account(wallet.UserId) + " · " + Loc.GetString("cmu-kaban-balance",
                ("balance", wallet.Balance), ("earned", wallet.Earned)));
        foreach (var item in _view.Items.Where(i => i.Kind is "voice" or "discord" && i.Status == "pending"))
        {
            var request = Card(_page, Account(item.Id), L("kind-" + item.Kind));
            Text(request, item.Description);
            if (item.Detail.Length > 0)
                Text(request, item.Detail);
            if (item.Kind == "voice")
                Button(request, L("approve-voice"), () => Submit(new KabanRequest
                    { Action = KabanAction.ApproveVoice, Target = item.Id, Text = item.Description }), primary: true);
            if (item.Kind == "discord")
                Button(request, L("approve-discord"), () => Submit(new KabanRequest
                    { Action = KabanAction.ApproveDiscord, Target = item.Id }), primary: true);
            Button(request, L("reject-refund"), () => Submit(new KabanRequest
                { Action = KabanAction.RejectPrivilege, Target = item.Id, Product = item.Kind }));
        }
        if (_view.Senior)
            SeniorAdmin();
        var auditLog = Card(_page, L("audit"));
        foreach (var audit in _view.Audit)
        {
            Text(auditLog, Date(audit.At) + " · " + audit.Description);
            Text(auditLog, audit.Id.ToString());
        }
    }

    private void AdjustBalance()
    {
        var form = Card(_page, L("adjust-title"), L("adjust-help"));
        Text(form, L("adjust-ckey"));
        var target = Input(form, L("adjust-ckey"));
        target.Text = _accountDrafts.GetValueOrDefault("adjust/ckey", "");
        target.OnTextChanged += _ => _accountDrafts["adjust/ckey"] = target.Text;
        var accounts = _view.Accounts.Values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToArray();
        var select = new OptionButton { HorizontalExpand = true, MinHeight = 34 };
        select.AddItem(L("select-account"), 0);
        for (var i = 0; i < accounts.Length; i++) select.AddItem(accounts[i], i + 1);
        select.OnItemSelected += e =>
        {
            select.SelectId(e.Id);
            if (e.Id > 0) target.Text = accounts[e.Id - 1];
        };
        form.AddChild(select);
        var delta = DraftInput(form, "adjust/delta", L("adjust-amount"));
        var reason = DraftInput(form, "adjust/reason", L("adjust-reason"));
        var apply = Button(form, L("adjust-apply"), () =>
        {
            if (!string.IsNullOrWhiteSpace(target.Text) && int.TryParse(delta.Text, out var value) && value != 0)
                Submit(new KabanRequest
                {
                    Action = KabanAction.CorrectBalance, TargetName = target.Text.Trim(), Amount = value,
                    Text = reason.Text, Confirmed = true,
                });
        }, primary: true);
        apply.Name = "AdjustBalanceSubmit";
        void RefreshApply() => apply.Disabled = string.IsNullOrWhiteSpace(target.Text) || target.Text.Trim().Length > 64
            || !int.TryParse(delta.Text, out var value) || value == 0;
        target.OnTextChanged += _ => RefreshApply();
        delta.OnTextChanged += _ => RefreshApply();
        RefreshApply();
    }

    private void SeniorAdmin()
    {
        var voice = Card(_page, L("kind-voice"), L("special-voice"));
        var voiceTarget = AccountInput(voice, "grant");
        var speaker = DraftInput(voice, "grant/speaker", L("speaker-id"));
        var consent = DraftInput(voice, "grant/consent", L("consent"));
        var grantConfirmed = DraftCheck(voice, "grant/confirmed", L("confirm-admin"));
        var grant = Button(voice, L("grant-voice"), () =>
        {
            if (grantConfirmed.Pressed && Guid.TryParse(voiceTarget.Text, out var id))
                Submit(new KabanRequest { Action = KabanAction.GrantVoice, Target = id,
                    Category = speaker.Text, Text = consent.Text, Confirmed = true });
        });
        void RefreshGrant() => grant.Disabled = !grantConfirmed.Pressed;
        grantConfirmed.OnToggled += _ => RefreshGrant();
        RefreshGrant();

        var settings = Card(_page, L("product-settings"));
        var product = new OptionButton { HorizontalExpand = true, MinHeight = 34 };
        for (var i = 0; i < _view.Products.Count; i++)
            product.AddItem(L("product-" + _view.Products[i].Id), i);
        _settingsProduct = Math.Clamp(_settingsProduct, 0, Math.Max(0, _view.Products.Count - 1));
        product.SelectId(_settingsProduct);
        settings.AddChild(product);
        var price = DraftInput(settings, "settings/price", L("account-price"),
            _view.Products[_settingsProduct].Price.ToString());
        product.OnItemSelected += e =>
        {
            product.SelectId(e.Id);
            _settingsProduct = e.Id;
            price.Text = _view.Products[e.Id].Price.ToString();
        };
        var disable = DraftCheck(settings, "settings/disabled", L("disable-product"));
        var confirm = DraftCheck(settings, "settings/confirmed", L("confirm-admin"));
        var save = Button(settings, L("save"), () =>
        {
            if (confirm.Pressed && int.TryParse(price.Text, out var value))
                Submit(new KabanRequest { Action = KabanAction.SetProduct, Product = _view.Products[product.SelectedId].Id,
                    Amount = value, Exceptional = disable.Pressed, Confirmed = true });
        });
        void RefreshSave() => save.Disabled = !confirm.Pressed;
        confirm.OnToggled += _ => RefreshSave();
        RefreshSave();

    }
}
