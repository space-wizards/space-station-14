using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed partial class EmitterSystem : SharedEmitterSystem
    {
        [Dependency] private RadioSystem _radio = default!;
        [Dependency] private NavMapSystem _navMap = default!;

        [SubscribeLocalEvent]
        private void OnDestruction(Entity<EmitterComponent> ent, ref DestructionEventArgs args)
        {
            // Engineering needs to know if an emitter is destroyed so they can replace it before the engine looses.
            AlertRadio(ent, ent.Comp.LocDestroyed);
        }

        // you shouldn't be able to deconstruct locked emitters but out of scope to fix
        [SubscribeLocalEvent]
        private void OnDeconstructed(Entity<EmitterComponent> ent, ref MachineDeconstructedEvent args)
        {
            // right now you don't even need to unlock the emitter to deconstruct it. that's almost certainly a bug but even without it it probably still needs an alert
            AlertRadio(ent, ent.Comp.LocDeconstructed);
        }
        [SubscribeLocalEvent]
        private void OnLockToggled(Entity<EmitterComponent> ent, ref LockToggledEvent args)
        {
            if (args.Locked)
                return;

            AlertRadio(ent, ent.Comp.LocUnlocked);
        }

        private void AlertRadio(Entity<EmitterComponent> ent, string locString)
        {
            //if (!ent.Comp.AlertRadio || !ent.Comp.IsOn || !ent.Comp.IsPowered)
            //    return; // APEs do not need to scream over engineering radio, and an emitter that is off is probably not going to be alerting radios

            var message = Loc.GetString(
                locString,
                ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner)))
            );
            _radio.SendRadioMessage(ent.Owner, message, ent.Comp.RadioChannel, ent.Owner);
        }
    }
}
