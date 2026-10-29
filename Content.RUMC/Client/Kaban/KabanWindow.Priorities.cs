using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RuCM.Kaban;

public sealed partial class KabanWindow
{
    private readonly Dictionary<Guid, HashSet<string>> _jobChoices = new();
    private bool _priorityDetails;

    internal static void QualifyJobNames(KabanState view)
    {
        var duplicates = view.Jobs.GroupBy(job => job.Value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.ToArray()).ToArray();
        if (duplicates.Length == 0) return;
        var departments = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<DepartmentPrototype>()
            .Where(department => department.IsCM && !department.Hidden && !department.EditorHidden)
            .OrderByDescending(department => department.Primary).ThenBy(department => department.ID).ToArray();
        foreach (var group in duplicates)
            foreach (var (id, name) in group)
            {
                var department = departments.First(candidate => candidate.Roles.Any(role => role.Id == id));
                var context = department.CustomName ?? Loc.GetString(department.Name);
                view.Jobs[id] = Loc.GetString("cmu-kaban-priority-qualified-job", ("job", name), ("department", context));
            }
    }

    private void Priorities()
    {
        Text(_page, L("priority-brief"));
        var tokens = _view.Items.Where(item => item.Kind == "token" && (item.Status is "available" or "reserved")
                && item.Expires > DateTime.UtcNow.Ticks)
            .OrderByDescending(item => item.Status == "reserved").ThenBy(item => item.Expires).ToArray();
        if (tokens.Length == 0)
        {
            Text(Card(_page), L("account-empty-priorities"));
            Button(_page, L("shop"), () => Navigate("shop"), primary: true);
            return;
        }
        if (!tokens.Any(item => item.Id == _token)) _token = tokens[0].Id;
        var selected = tokens.First(item => item.Id == _token);
        var maximum = selected.Flexible ? 3 : 1;
        var tokenCard = Card(_page, L("priority-step-token"));
        var picker = new OptionButton { HorizontalExpand = true, MinHeight = 36, Name = "PriorityToken" };
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            picker.AddItem(Loc.GetString("cmu-kaban-priority-token-label", ("index", index + 1),
                ("weight", token.Multiplier), ("count", token.Flexible ? 3 : 1), ("date", Date(token.Expires))), index);
        }
        picker.SelectId(Array.FindIndex(tokens, item => item.Id == _token));
        picker.OnItemSelected += args => { _token = tokens[args.Id].Id; Render(); };
        tokenCard.AddChild(picker);
        if (!_view.PrioritiesEnabled) Text(tokenCard, L("priorities-disabled"), Warning);
        if (!_view.Lobby) Text(tokenCard, L("lobby-only"), Warning);

        if (selected.Status == "reserved")
        {
            Text(Card(_page, L("priority-armed"), accent: Gold), ItemText(selected));
            Text(_page, L("priority-armed-help"));
            Button(_actions, L("disarm"), () => Submit(new KabanRequest
                { Action = KabanAction.Disarm, Item = selected.Id }), disabled: !_view.Lobby);
            return;
        }
        if (!_jobChoices.TryGetValue(selected.Id, out var choices))
        {
            choices = new HashSet<string>((selected.Jobs ?? []).Where(_view.Jobs.ContainsKey));
            _jobChoices[selected.Id] = choices;
        }
        choices.RemoveWhere(id => !_view.Jobs.ContainsKey(id));
        var jobsCard = Card(_page, L("priority-step-job"), L(selected.Flexible ? "priority-flex-help" : "priority-single-help"));
        var search = Input(jobsCard, L("search-job"));
        search.Text = _accountDrafts.GetValueOrDefault("jobs/search", "");
        search.OnTextChanged += _ => _accountDrafts["jobs/search"] = search.Text;
        search.Name = "PrioritySearch";
        var jobBox = Column();
        jobsCard.AddChild(jobBox);
        var footer = Card(_actions, L("priority-step-activate"));
        var summary = Text(footer, "", Ink);
        var otherArmed = tokens.Any(item => item.Status == "reserved" && item.Id != selected.Id);
        if (otherArmed) Text(footer, L("priority-other-armed"), Warning);
        var buttons = Row();
        footer.AddChild(buttons);
        Button(buttons, L("priority-clear"), () => { choices.Clear(); Render(); });
        var activate = Button(buttons, Loc.GetString("cmu-kaban-priority-activate", ("weight", selected.Multiplier)),
            () => Submit(new KabanRequest { Action = KabanAction.Arm, Item = selected.Id, Jobs = choices.Order().ToArray() }), primary: true);
        activate.Name = "PriorityActivate";
        var checks = new List<(string Id, CheckBox Check)>();
        void RefreshChoices()
        {
            var names = choices.OrderBy(id => _view.Jobs[id]).Select(id => _view.Jobs[id]).ToArray();
            summary.SetMessage(FormattedMessage.FromUnformatted(
                Loc.GetString("cmu-kaban-account-selected-jobs", ("count", choices.Count), ("limit", maximum))
                + "\n" + (names.Length == 0 ? L("priority-select-job") : string.Join(" · ", names))), Ink);
            activate.Disabled = choices.Count == 0 || choices.Count > maximum || !_view.PrioritiesEnabled || !_view.Lobby || otherArmed;
            foreach (var (id, check) in checks)
            {
                check.Pressed = choices.Contains(id);
                check.Disabled = maximum > 1 && !choices.Contains(id) && choices.Count >= maximum;
            }
        }
        void Jobs()
        {
            jobBox.RemoveAllChildren();
            checks.Clear();
            foreach (var (id, name) in _view.Jobs.OrderBy(job => job.Value)
                         .Where(job => job.Value.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var check = new CheckBox
                {
                    MinHeight = 34, HorizontalExpand = true, Name = "PriorityRole-" + id, ToolTip = name,
                };
                check.Label.Visible = false;
                // Keep wrapping text inside the checkbox so clicking the role name also toggles it.
                Text((BoxContainer) check.Label.Parent!, name, Ink).MouseFilter = Control.MouseFilterMode.Ignore;
                check.OnToggled += args =>
                {
                    if (args.Pressed)
                    {
                        if (maximum == 1) choices.Clear();
                        choices.Add(id);
                    }
                    else choices.Remove(id);
                    RefreshChoices();
                };
                jobBox.AddChild(check);
                checks.Add((id, check));
            }
            if (checks.Count == 0) Text(jobBox, L("priority-no-jobs"));
            RefreshChoices();
        }
        search.OnTextChanged += _ => Jobs();
        Jobs();
        Button(_page, L("priority-details"), () => { _priorityDetails = !_priorityDetails; Render(); });
        if (_priorityDetails)
        {
            var rules = Card(_page);
            Text(rules, L("priority-intro"));
            Text(rules, L("priority-rules"));
        }
    }
}
