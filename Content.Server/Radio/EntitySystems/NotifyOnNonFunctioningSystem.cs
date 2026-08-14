using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio.Components;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// System for sending radio notification upon entity becoming
/// non-functioning - unpowered / deconstructed / destroyed.
/// </summary>
public sealed partial class NotifyOnNonFunctioningSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PowerStateSystem _powerState = default!;

    /// <summary> Notify on entity destruction. </summary>
    [SubscribeLocalEvent]
    private void OnDestruction(Entity<NotifyOnNonFunctioningComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.LocDestroyed.HasValue)
            AlertRadioIfWasWorking(ent, ent.Comp.LocDestroyed);
    }

    /// <summary> Notify on deconstruction. </summary>
    [SubscribeLocalEvent]
    private void OnDeconstructed(Entity<NotifyOnNonFunctioningComponent> ent, ref MachineDeconstructedEvent args)
    {
        if (ent.Comp.LocDeconstructed.HasValue)
            AlertRadioIfWasWorking(ent, ent.Comp.LocDeconstructed);
    }

    /// <summary> Notify on unlocking already locked entity. </summary>
    [SubscribeLocalEvent]
    private void OnLockToggled(Entity<NotifyOnNonFunctioningComponent> ent, ref LockToggledEvent args)
    {
        if (args.Locked || !ent.Comp.LocUnlocked.HasValue)
            return;

        AlertRadioIfWasWorking(ent, ent.Comp.LocUnlocked);
    }

    /// <summary> Notify on turning off. </summary>
    [SubscribeLocalEvent]
    private void OnIsWorkingChanges(Entity<NotifyOnNonFunctioningComponent> ent, ref PowerStateChanged args)
    {
        // deleted entity is working change should be handled during other events
        if (args.IsWorking || !ent.Comp.LocTurnedOff.HasValue || TerminatingOrDeleted(ent))
            return;

        AlertRadio(ent, ent.Comp.LocTurnedOff);
    }

    /// <summary> Notify on unanchoring. </summary>
    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<NotifyOnNonFunctioningComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored || !ent.Comp.LocUnanchored.HasValue || TerminatingOrDeleted(ent))
            return;

        AlertRadioIfWasWorking(ent, ent.Comp.LocUnanchored);
    }

    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<NotifyOnNonFunctioningComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!ent.Comp.LocUnpowered.HasValue ||!_powerState.GetWorkingState(ent.Owner))
            return;

        if (args.ReceivedPower < args.DrawRate)
            AlertRadioIfWasWorking(ent, ent.Comp.LocUnpowered);
    }

    private void AlertRadioIfWasWorking(Entity<NotifyOnNonFunctioningComponent> ent, string locString)
    {

        if (!_powerState.GetWorkingState(ent.Owner))
            return;

        AlertRadio(ent, locString);
    }

    private void AlertRadio(Entity<NotifyOnNonFunctioningComponent> ent, string locString)
    {
        if (ent.Comp.RequirePowered)
        {
            if (TryComp<ApcPowerReceiverComponent>(ent, out var apc) && !apc.Powered)
                return;

            if (TryComp<PowerConsumerComponent>(ent, out var consumer) && consumer.DrawRate < consumer.ReceivedPower)
                return;
        }

        var locationInfo = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner));
        var message = Loc.GetString(locString, ("location", locationInfo));
        _radio.SendRadioMessage(ent.Owner, message, ent.Comp.RadioChannel, ent.Owner);
    }
}
