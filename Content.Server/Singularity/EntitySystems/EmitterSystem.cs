using Content.Shared.DeviceLinking.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Singularity.EntitySystems;

public sealed partial class EmitterSystem : SharedEmitterSystem
{
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<EmitterComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.SetTypePorts.TryGetValue(args.Port, out var boltType)
            && TryComp<NetworkPoweredAmmoProviderComponent>(ent, out var ammoProvider))
        {
            ammoProvider.Prototype = boltType;
        }
    }
}
