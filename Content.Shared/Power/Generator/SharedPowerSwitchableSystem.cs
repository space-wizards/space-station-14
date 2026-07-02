using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

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
        SubscribeLocalEvent<PowerSwitchableComponent, GetVerbsEvent<InteractionVerb>>(GetVerbs);
    }

    private void OnExamined(EntityUid uid, PowerSwitchableComponent comp, ExaminedEvent args)
    {
        // Show which voltage is currently selected.
        var voltage = VoltageColor(GetVoltage(uid, comp));
        args.PushMarkup(Loc.GetString(comp.ExamineText, ("voltage", voltage)));
    }

    private void GetVerbs(EntityUid uid, PowerSwitchableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var voltage = VoltageColor(GetNextVoltage(uid, comp));
        var msg = Loc.GetString("power-switchable-switch-voltage", ("voltage", voltage));

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                // don't need to check it again since if its disabled server wont let the verb act
                Cycle(uid, args.User, comp);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Text = msg
        };

        var ev = new SwitchPowerCheckEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.DisableMessage != null)
        {
            verb.Message = ev.DisableMessage;
            verb.Disabled = true;
        }

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Cycles voltage then updates nodes and optionally power supplier to match it.
    /// </summary>
    public virtual void Cycle(EntityUid uid, EntityUid user, PowerSwitchableComponent? comp = null) { }

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

/// <summary>
/// Raised on a <see cref="PowerSwitchableComponent"/> to see if its verb should work.
/// If <see cref="DisableMessage"/> is non-null, the verb is disabled with that as the message.
/// </summary>
[ByRefEvent]
public record struct SwitchPowerCheckEvent(string? DisableMessage = null);
