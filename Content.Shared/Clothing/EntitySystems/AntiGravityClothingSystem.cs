using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class AntiGravityClothingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
    }

    private void OnIsWeightless(Entity<AntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        args.Args.Handled = true;
        args.Args.IsWeightless = true;
    }
}
