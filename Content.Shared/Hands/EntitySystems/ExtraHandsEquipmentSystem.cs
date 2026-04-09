using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Hands.EntitySystems;

public sealed class ExtraHandsEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked as part of the same game state.

        if (!TryComp<HandsComponent>(args.Equipee, out var handsComp))
            return;

        foreach (var (handName, hand) in ent.Comp.Hands)
        {
            // add the NetEntity id to the container name to prevent multiple items with this component from conflicting
            var handId = $"{GetNetEntity(ent.Owner).Id}-{handName}";
            _hands.AddHand((args.Equipee, handsComp), handId, hand.Location);
        }
    }

    private void OnUnequipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked as part of the same game state.

        if (!TryComp<HandsComponent>(args.Equipee, out var handsComp))
            return;

        foreach (var handName in ent.Comp.Hands.Keys)
        {
            // add the NetEntity id to the container name to prevent multiple items with this component from conflicting
            var handId = $"{GetNetEntity(ent.Owner).Id}-{handName}";
            _hands.RemoveHand((args.Equipee, handsComp), handId);
        }
    }
}
