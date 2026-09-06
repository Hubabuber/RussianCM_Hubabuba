using Content.Client.Message;
using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCPortableGeneratorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color GreenColor = Color.FromHex("#5AC229");
    private static readonly Color RedColor = Color.FromHex("#CE3E31");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");

    [ViewVariables]
    private RMCPortableGeneratorWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCPortableGeneratorWindow>();

        _window.ToggleButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorToggleBuiMsg());
        _window.EjectButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorEjectFuelBuiMsg());
        _window.RaisePowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorRaisePowerBuiMsg());
        _window.LowerPowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorLowerPowerBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCPortableGeneratorComponent? gen))
            return;

        if (gen.On)
        {
            // RuMC edit start
            _window.StatusLabel.SetMarkupPermissive($"[color={GreenColor.ToHex()}]{Loc.GetString("rmc-portable-generator-window-status-online")}[/color]");
            _window.ToggleButton.Text = Loc.GetString("rmc-portable-generator-window-stop");
            // RuMC edit end
        }
        else
        {
            // RuMC edit start
            _window.StatusLabel.SetMarkupPermissive($"[color={RedColor.ToHex()}]{Loc.GetString("rmc-portable-generator-window-status-offline")}[/color]");
            _window.ToggleButton.Text = Loc.GetString("rmc-portable-generator-window-start");
            // RuMC edit end
        }

        var fuelPercent = gen.Sheets > 0 ? gen.SheetFraction * 100 : 0;
        // RuMC edit start
        _window.FuelLabel.SetMarkupPermissive(Loc.GetString("rmc-portable-generator-window-fuel-line",
            ("sheets", gen.Sheets),
            ("fuel", Loc.GetString(gen.FuelName)),
            ("percent", $"{fuelPercent:F0}")));
        // RuMC edit end

        _window.FuelBar.MinValue = 0;
        _window.FuelBar.MaxValue = gen.MaxSheets;
        _window.FuelBar.Value = gen.Sheets;
        _window.FuelBarLabel.Text = $"{gen.Sheets} / {gen.MaxSheets}";

        _window.EjectButton.Disabled = gen.On;

        var watts = gen.Watts * gen.PowerGenPercent / 100;
        // RuMC edit start
        _window.PowerOutputLabel.SetMarkupPermissive(Loc.GetString("rmc-portable-generator-window-power-line",
            ("watts", watts),
            ("percent", gen.PowerGenPercent)));
        // RuMC edit end

        _window.LowerPowerButton.Disabled = gen.PowerGenPercent <= gen.MinPowerPercent;
        _window.RaisePowerButton.Disabled = gen.PowerGenPercent >= gen.MaxPowerPercent;

        _window.HeatBar.MinValue = 0;
        _window.HeatBar.MaxValue = gen.OverheatThreshold;
        _window.HeatBar.Value = Math.Min(gen.Heat, gen.OverheatThreshold);

        string heatStatus;
        if (gen.Heat > 200)
            heatStatus = $"[color={RedColor.ToHex()}]{Loc.GetString("rmc-portable-generator-window-heat-danger")}[/color]"; // RuMC edit
        else if (gen.Heat >= 100)
            heatStatus = $"[color={OrangeColor.ToHex()}]{Loc.GetString("rmc-portable-generator-window-heat-caution")}[/color]"; // RuMC edit
        else
            heatStatus = $"[color={GreenColor.ToHex()}]{Loc.GetString("rmc-portable-generator-window-heat-nominal")}[/color]"; // RuMC edit

        _window.HeatStatusLabel.SetMarkupPermissive(Loc.GetString("rmc-portable-generator-window-heat-line", ("status", heatStatus))); // RuMC edit
    }
}
