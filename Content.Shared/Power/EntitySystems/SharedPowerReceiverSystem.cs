using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Power.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public abstract bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref SharedApcPowerReceiverComponent? component);

    public void SetNeedsPower(EntityUid uid, bool value, SharedApcPowerReceiverComponent? receiver = null)
    {
        if (!ResolveApc(uid, ref receiver) || receiver.NeedsPower == value)
            return;

        receiver.NeedsPower = value;
        Dirty(uid, receiver);
    }

    public void SetPowerDisabled(EntityUid uid, bool value, SharedApcPowerReceiverComponent? receiver = null)
    {
        if (!ResolveApc(uid, ref receiver) || receiver.PowerDisabled == value)
            return;

        receiver.PowerDisabled = value;
        Dirty(uid, receiver);
    }

    /// <summary>
    /// Turn this machine on or off.
    /// Returns true if we turned it on, false if we turned it off.
    /// </summary>
    public bool TogglePower(EntityUid uid, bool playSwitchSound = true, SharedApcPowerReceiverComponent? receiver = null, EntityUid? user = null)
    {
        if (!ResolveApc(uid, ref receiver))
            return true;

        // it'll save a lot of confusion if 'always powered' means 'always powered'
        if (!receiver.NeedsPower)
        {
            SetPowerDisabled(uid, false, receiver);
            return true;
        }

        SetPowerDisabled(uid, !receiver.PowerDisabled, receiver);

        if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} hit power button on {ToPrettyString(uid)}, it's now {(!receiver.PowerDisabled ? "on" : "off")}");

        if (playSwitchSound)
        {
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"), uid, user: user,
                AudioParams.Default.WithVolume(-2f));
        }

        return !receiver.PowerDisabled; // i.e. PowerEnabled
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
