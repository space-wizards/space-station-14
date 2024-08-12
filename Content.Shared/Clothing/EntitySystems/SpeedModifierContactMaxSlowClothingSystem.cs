using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class SpeedModifierContactMaxSlowClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedModifierContactMaxSlowClothingComponent, InventoryRelayedEvent<GetSpeedModifierContactMaxSlowEvent>>(OnGetMaxSlow);
    }

    private void OnGetMaxSlow(Entity<SpeedModifierContactMaxSlowClothingComponent> ent, ref InventoryRelayedEvent<GetSpeedModifierContactMaxSlowEvent> args)
    {
        args.Args.SetIfMax(ent.Comp.MaxContactSprintSlowdown, ent.Comp.MaxContactWalkSlowdown);
    }
}
