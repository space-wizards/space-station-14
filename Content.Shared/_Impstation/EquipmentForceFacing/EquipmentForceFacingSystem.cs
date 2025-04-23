using Content.Shared.Inventory;
using Content.Shared.Mobs;

namespace Content.Shared._Impstation.EquipmentForceFacing;

public sealed class EquipmentForceFacingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentForceFacingComponent, InventoryRelayedEvent<MobStateChangedEvent>>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<EquipmentForceFacingComponent> ent, ref InventoryRelayedEvent<MobStateChangedEvent> args)
    {
        if (args.Args.NewMobState == MobState.Alive)
            return;

        _transform.SetWorldRotationNoLerp(args.Args.Target, Angle.FromDegrees(0));
    }
}
