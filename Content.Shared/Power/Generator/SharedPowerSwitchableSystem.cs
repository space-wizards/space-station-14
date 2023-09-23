using Content.Shared.Examine;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Shared logic for power-switchable devices.
/// </summary>
/// <seealso cref="PowerSwitchableComponent"/>
public abstract class SharedPowerSwitchableSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PowerSwitchableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, PowerSwitchableComponent comp, ExaminedEvent args)
    {
        // Show which voltage is currently selected.
        var voltage = VoltageColor(GetVoltage(uid, comp));
        args.PushMarkup(Loc.GetString(comp.ExamineText, ("voltage", voltage)));
    }

    /// <summary>
    /// Helper to get the colored markup string for a voltage type.
    /// </summary>
    public string VoltageColor(SwitchableVoltage voltage)
    {
        return Loc.GetString("power-switchable-voltage", ("voltage", VoltageString(voltage)));
    }

    /// <summary>
    /// Converts from "hv" to "HV" since for some reason the enum gets made lowercase???
    /// </summary>
    public string VoltageString(SwitchableVoltage voltage)
    {
        return voltage.ToString().ToUpper();
    }

    /// <summary>
    /// Returns index of the next cable type index to cycle to.
    /// </summary>
    public int NextIndex(EntityUid uid, PowerSwitchableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0;

        // loop back at the end
        return (comp.ActiveIndex + 1) % comp.Cables.Count;
    }

    /// <summary>
    /// Returns the current cable voltage being used by a power-switchable device.
    /// </summary>
    public SwitchableVoltage GetVoltage(EntityUid uid, PowerSwitchableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return default;

        return comp.Cables[comp.ActiveIndex].Voltage;
    }

    /// <summary>
    /// Returns the cable's next voltage to cycle to being used by a power-switchable device.
    /// </summary>
    public SwitchableVoltage GetNextVoltage(EntityUid uid, PowerSwitchableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return default;

        return comp.Cables[NextIndex(uid, comp)].Voltage;
    }
}
