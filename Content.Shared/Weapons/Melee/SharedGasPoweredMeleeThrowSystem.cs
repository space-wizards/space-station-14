using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// This component makes entity require gas in order to throw with melee.
/// </summary>
public abstract class SharedGasPoweredMeleeThrowSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GasPoweredThrowerComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
    }

    private void OnAttemptMeleeThrowOnHit(Entity<GasPoweredThrowerComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        if (!Container.TryGetContainer(ent, ent.Comp.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is null)
        {
            args.Cancelled = true;
        }

    }

}
