using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

/// <inheritdoc cref="AccentWearerNameClothingComponent"/>
public sealed class AccentWearerNameClothingSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentWearerNameClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AccentWearerNameClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<AccentWearerNameClothingComponent, InventoryRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
    }

    private void OnGotEquipped(Entity<AccentWearerNameClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _nameMod.RefreshNameModifiers(args.Wearer);
    }

    private void OnGotUnequipped(Entity<AccentWearerNameClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _nameMod.RefreshNameModifiers(args.Wearer);
    }

    private void OnRefreshNameModifiers(Entity<AccentWearerNameClothingComponent> ent, ref InventoryRelayedEvent<RefreshNameModifiersEvent> args)
    {
        var ev = new AccentGetEvent(ent, args.Args.BaseName);
        RaiseLocalEvent(ent, ev);
        // Use a negative priority since we're going to bulldoze any earlier changes
        args.Args.AddModifier("comp-accent-wearer-name-clothing-format", -1, ("accentedName", ev.Message));
    }
}
