using Content.Shared.Damage.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnPickupSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        ///SubscribeLocalEvent<DamageOnPickupComponent, ContainerGettingInsertedAttemptEvent>(OnHandPick);
    }

    private void OnHandPickup(Entity<DamageOnPickupComponent> entity, ref ContainerGettingInsertedAttemptEvent args)
    {

    }
}
