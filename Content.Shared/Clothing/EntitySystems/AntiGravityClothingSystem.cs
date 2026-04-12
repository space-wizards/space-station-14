using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Standing;

namespace Content.Shared.Clothing.EntitySystems;

/// <remarks>
/// We check standing state on all clothing because we don't want you to have anti-gravity unless you're standing.
/// This is for balance reasons as it prevents you from wearing anti-grav clothing to cheese being stun cuffed, as
/// well as other worse things.
/// </remarks>
public sealed class AntiGravityClothingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<DownedEvent>>(OnDowned);
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<StoodEvent>>(OnStood);
    }

    private void OnIsWeightless(Entity<AntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled || _standing.IsDown(args.Owner))
            return;

        args.Args.Handled = true;
        args.Args.IsWeightless = true;
    }

    private void OnEquipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotEquippedEvent args)
    {
        // This clothing item does nothing if we're not standing
        if (_standing.IsDown(args.Wearer))
            return;

        _gravity.RefreshWeightless(args.Wearer, true);
    }

    private void OnUnequipped(Entity<AntiGravityClothingComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        // This clothing item does nothing if we're not standing
        if (_standing.IsDown(args.Wearer))
            return;

        _gravity.RefreshWeightless(args.Wearer, false);
    }

    private void OnDowned(Entity<AntiGravityClothingComponent> entity, ref InventoryRelayedEvent<DownedEvent> args)
    {
        _gravity.RefreshWeightless(args.Owner, false);
    }

    private void OnStood(Entity<AntiGravityClothingComponent> entity, ref InventoryRelayedEvent<StoodEvent> args)
    {
        _gravity.RefreshWeightless(args.Owner, true);
    }
}
