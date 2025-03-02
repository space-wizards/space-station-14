using System.Diagnostics.CodeAnalysis;
using Content.Shared.Power.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public abstract bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref SharedApcPowerReceiverComponent? component);

    /// <summary>
    /// Turn this machine on or off.
    /// Returns true if we turned it on, false if we turned it off.
    /// </summary>
    public bool TogglePower(EntityUid uid, bool playSwitchSound = true, SharedApcPowerReceiverComponent? receiver = null, EntityUid? user = null)
    {
        if (!ResolveApc(uid, ref receiver))
            return true;

        var oldValue = receiver.PowerDisabled;

        // it'll save a lot of confusion if 'always powered' means 'always powered'
        if (!receiver.NeedsPower)
        {
            receiver.PowerDisabled = false;
        }
        else
        {
            receiver.PowerDisabled = !receiver.PowerDisabled;

            if (user != null)
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} hit power button on {ToPrettyString(uid)}, it's now {(!receiver.PowerDisabled ? "on" : "off")}");

            if (playSwitchSound)
            {
                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"), uid, user,
                    AudioParams.Default.WithVolume(-2f));
            }
        }

        if (oldValue != receiver.PowerDisabled)
        {
            Dirty(uid, receiver);
            RaisePower((uid, receiver));
        }

        return !receiver.PowerDisabled; // i.e. PowerEnabled
    }

    protected virtual void RaisePower(Entity<SharedApcPowerReceiverComponent> entity)
    {
        // NOOP on server because client has 0 idea of load so we can't raise it properly in shared.
    }

	/// <summary>
	/// Checks if entity is APC-powered device, and if it have power.
    /// </summary>
    public bool IsPowered(Entity<SharedApcPowerReceiverComponent?> entity)
    {
        if (!ResolveApc(entity.Owner, ref entity.Comp))
            return true;

        return entity.Comp.Powered;
    }

    protected string GetExamineText(bool powered)
    {
        return Loc.GetString("power-receiver-component-on-examine-main",
                                ("stateText", Loc.GetString(powered
                                    ? "power-receiver-component-on-examine-powered"
                                    : "power-receiver-component-on-examine-unpowered")));
    }
}
