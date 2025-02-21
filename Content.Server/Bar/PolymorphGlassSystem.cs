using Content.Shared.Bar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Bar;

public sealed class PolymorphGlassSystem : SharedPolymorphGlassSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionsSystem = default!;

    protected override void ChangeGlass(EntityUid uid, PolymorphGlassComponent component, EntityPrototype prototype, GetVerbsEvent<Verb> args)
    {
        if (!_solutionsSystem.TryGetSolution(uid, "drink", out var sourceSolutionEntity, out var sourceSolution))
            return;

        //  The original glass needs to be deleted before spawning the new one because I'm using SpawnInContainer
        //  Otherwise if you are holding the glass and change type it won't stay in your hand.
        //  So I'm copying all the stuff that needs to be transfered from the old to the new glass to use later.
        //  Here I copy the solution. I also cache some bools.
        Solution solution = sourceSolution.Clone();
        var position = Transform(uid).Coordinates;
        var inContainer = _containerSystem.IsEntityInContainer(uid);
        var hasItemSlots = TryComp<ItemSlotsComponent>(uid, out var itemSlotsComp);

        //  Here I copy the garnishes, if there are any, and eject them so they won't get deleted with the glass.
        EntityUid? sourceGarnish = null;
        EntityUid? sourceDecoration = null;

        if (hasItemSlots)
        {
            if (_itemSlotsSystem.TryGetSlot(uid, "garnish_slot", out var sourceGarnishSlot, itemSlotsComp))
                _itemSlotsSystem.TryEject(uid, sourceGarnishSlot, null, out sourceGarnish, true);
            if (_itemSlotsSystem.TryGetSlot(uid, "decoration_slot", out var sourceDecorationSlot, itemSlotsComp))
                _itemSlotsSystem.TryEject(uid, sourceDecorationSlot, null, out sourceDecoration, true);
        }

        //  Trying to keep my vars in scope.
        EntityUid? targetEntity = null;

        // If the entity was in a container (e.g. Hands) I try getting it so i can spawn the new glass in it as well.
        if (inContainer)
        {
            var containingUid = position.EntityId;
            if (_containerSystem.TryGetContainingContainer(containingUid, uid, out var container))
            {
                //  Deleting the entity right before spawning so it won't spawn on the ground.
                Del(uid);
                targetEntity = SpawnInContainerOrDrop(prototype.ID, containingUid, container.ID);
            }
        }
        else
        {
            Del(uid);
            targetEntity = Spawn(prototype.ID, position);
        }

        if (!targetEntity.HasValue)
            return;

        if (!_solutionsSystem.TryGetSolution(targetEntity.Value, "drink", out var targetSolutionEntity, out var targetSolution))
            return;

        //  Transfer the source solution into the new glass.
        _solutionsSystem.AddSolution(targetSolutionEntity.Value, solution);

        // Transfer the garnishes to the new glass, if there were any.
        if (hasItemSlots)
        {
            if (TryComp<ItemSlotsComponent>(targetEntity, out var targetItemSlots))
            {
                if (sourceGarnish.HasValue && _itemSlotsSystem.TryGetSlot(targetEntity.Value, "garnish_slot", out var targetGarnishSlot, targetItemSlots))
                    _itemSlotsSystem.TryInsert(targetEntity.Value, "garnish_slot", sourceGarnish.Value, args.User, targetItemSlots, true);
                if (sourceDecoration.HasValue && _itemSlotsSystem.TryGetSlot(targetEntity.Value, "decoration_slot", out var targetDecorationSlot, targetItemSlots))
                    _itemSlotsSystem.TryInsert(targetEntity.Value, "decoration_slot", sourceDecoration.Value, null, targetItemSlots, true);
            }
        }

    }

}
