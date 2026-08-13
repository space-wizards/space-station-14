using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio.Components;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class NotifyOnNonFunctioningSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PowerStateSystem _powerState = default!;

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<NotifyOnNonFunctioningComponent> ent, ref DestructionEventArgs args)
    {
        if(ent.Comp.LocDestroyed.HasValue)
            AlertRadioIfWasWorking(ent, ent.Comp.LocDestroyed);
    }

    [SubscribeLocalEvent]
    private void OnDeconstructed(Entity<NotifyOnNonFunctioningComponent> ent, ref MachineDeconstructedEvent args)
    {
        if(ent.Comp.LocDeconstructed.HasValue)
            AlertRadioIfWasWorking(ent, ent.Comp.LocDeconstructed);
    }

    [SubscribeLocalEvent]
    private void OnLockToggled(Entity<NotifyOnNonFunctioningComponent> ent, ref LockToggledEvent args)
    {
        if (args.Locked || !ent.Comp.LocUnlocked.HasValue)
            return;

        AlertRadioIfWasWorking(ent, ent.Comp.LocUnlocked);
    }

    [SubscribeLocalEvent]
    private void OnIsWorkingChanges(Entity<NotifyOnNonFunctioningComponent> ent, ref PowerStateChanged args)
    {
        if (args.IsWorking || !ent.Comp.LocUnpowered.HasValue)
            return;

        AlertRadio(ent, ent.Comp.LocUnpowered);
    }

    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<NotifyOnNonFunctioningComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored || !ent.Comp.LocUnpowered.HasValue)
            return;

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
            if(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && !apc.Powered)
                return;

            if (TryComp<PowerConsumerComponent>(ent, out var consumer) && consumer.DrawRate < consumer.ReceivedPower)
                return;
        }

        var message = Loc.GetString(
            locString,
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner)))
        );
        _radio.SendRadioMessage(ent.Owner, message, ent.Comp.RadioChannel, ent.Owner);
    }
}
