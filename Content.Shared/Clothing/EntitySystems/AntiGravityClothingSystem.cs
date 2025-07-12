using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class AntiGravityClothingSystem : EntitySystem
{
    [Dependency] SharedGravitySystem _gravity = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnIsWeightless(Entity<AntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        args.Args.Handled = true;
        args.Args.IsWeightless = true;
    }

    private void OnEquipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotEquippedEvent args)
    {
        _gravity.RefreshWeightless(args.Wearer, true);
    }

    private void OnUnequipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        _gravity.RefreshWeightless(args.Wearer, false);
    }
}
