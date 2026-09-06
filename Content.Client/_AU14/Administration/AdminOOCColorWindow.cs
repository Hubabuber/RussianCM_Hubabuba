using System.Numerics;
using Content.Client._AU14.UI;
using Content.Shared._AU14.Administration;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client._AU14.Administration;

public sealed class AdminOOCColorWindow : DefaultWindow
{
    public event Action<int, string?>? OnSetColor;

    private readonly BoxContainer _rankList;

    public AdminOOCColorWindow()
    {
        Title = Loc.GetString("au14-admin-ooc-color-title");
        MinSize = new Vector2(620, 420);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(8),
        };
        root.AddChild(new Label { Text = Loc.GetString("au14-admin-ooc-color-description") });

        _rankList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(_rankList);
        root.AddChild(scroll);

        var panel = new PanelContainer
        {
            PanelOverride = GmodStyle.Panel(GmodStyle.PanelBg),
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        panel.AddChild(root);
        Contents.AddChild(panel);
        GmodStyle.RecolorKeyLabels(panel);
    }

    public void Populate(AdminOOCColorEuiState state)
    {
        _rankList.RemoveAllChildren();

        if (state.IsLoading)
        {
            _rankList.AddChild(new Label { Text = Loc.GetString("au14-admin-ooc-color-loading") });
            return;
        }

        if (state.Ranks.Count == 0)
        {
            _rankList.AddChild(new Label { Text = Loc.GetString("au14-admin-ooc-color-no-groups") });
            return;
        }

        foreach (var rank in state.Ranks)
            AddRankRow(rank);
    }

    private void AddRankRow(AdminOOCColorRank rank)
    {
        var name = new Label
        {
            Text = rank.Name,
            MinWidth = 180,
            VerticalAlignment = VAlignment.Center,
        };
        var input = new LineEdit
        {
            Text = rank.Color ?? string.Empty,
            PlaceHolder = Loc.GetString("au14-admin-ooc-color-placeholder"),
            HorizontalExpand = true,
        };
        var preview = new PanelContainer
        {
            MinSize = new Vector2(42, 28),
            PanelOverride = GmodStyle.Panel(GmodStyle.FieldBg),
        };
        var save = new Button { Text = Loc.GetString("au14-admin-ooc-color-save") };
        var reset = new Button { Text = Loc.GetString("au14-admin-ooc-color-reset") };
        var invalid = new Label
        {
            Text = Loc.GetString("au14-admin-ooc-color-invalid"),
            FontColorOverride = Color.Red,
            Visible = false,
            VerticalAlignment = VAlignment.Center,
        };

        Color? ParseColor()
        {
            var value = input.Text.Trim();
            return value.Length == 0 ? null : Color.TryFromHex(value, out var color) ? color : null;
        }

        void RefreshPreview()
        {
            var value = input.Text.Trim();
            var parsed = ParseColor();
            var valid = value.Length == 0 || parsed.HasValue;
            preview.PanelOverride = GmodStyle.Panel(parsed ?? GmodStyle.FieldBg);
            invalid.Visible = !valid;
            save.Disabled = !valid;
        }

        input.OnTextChanged += _ => RefreshPreview();
        save.OnPressed += _ =>
        {
            var value = input.Text.Trim();
            if (value.Length == 0)
            {
                OnSetColor?.Invoke(rank.Id, null);
                return;
            }

            if (!Color.TryFromHex(value, out var color))
                return;

            OnSetColor?.Invoke(rank.Id, color.ToHex());
        };
        reset.OnPressed += _ => OnSetColor?.Invoke(rank.Id, null);

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 6),
        };
        row.AddChild(name);
        row.AddChild(input);
        row.AddChild(preview);
        row.AddChild(save);
        row.AddChild(reset);
        row.AddChild(invalid);
        GmodStyle.Modernize(save);
        GmodStyle.Modernize(reset);
        _rankList.AddChild(row);
        RefreshPreview();
    }
}
