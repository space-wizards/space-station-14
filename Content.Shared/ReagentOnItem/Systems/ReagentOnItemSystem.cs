using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Fluids;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Content.Shared.NameModifier.Components;
using Content.Shared.NameModifier.EntitySystems;

namespace Content.Shared.ReagentOnItem;

/// <summary>
///     Deals with items that apply a solution on use to other entities.
/// </summary>
public abstract class ReagentOnItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    ///     Tries to apply the given solution to the item.
    ///     It will apply all reagents that can stick and will spill anything
    ///     that cannot stick to the floor.
    /// </summary>
    /// <returns> False if there is no solution or if entity isn't an item and true otherwise. </returns>
    public bool ApplyReagentEffectToItem(Entity<ItemComponent> item, string reagent, FixedPoint2 quantity, ReagentOnItemComponent comp)
    {
        if (quantity <= 0)
        {
            return false;
        }

        // This is very specific, so I don't think it needs to use an event.
        if (HasComp<NonStickSurfaceComponent>(item))
        {
            _popup.PopupEntity(Loc.GetString("non-stick-gloves-reagent-falls-off", ("target", Identity.Entity(item, EntityManager))), item);
            _puddle.TrySpillAt(item, new Solution(reagent, quantity), out var _, false);
            return false;
        }

        ConvertReagentToStacks(item, comp, reagent, quantity);

        return true;
    }

    /// <summary>
    ///     Convert the reagent to stacks and add them to the component.
    ///     Will put any extra reagent that couldn't be applied in a puddle.
    /// </summary>
    private void ConvertReagentToStacks(EntityUid item, ReagentOnItemComponent comp, string reagentProto, FixedPoint2 reagentQuantity)
    {
        var total = comp.EffectStacks + reagentQuantity;

        comp.EffectStacks = FixedPoint2.Min(total, comp.MaxStacks);

        if (total > comp.MaxStacks)
            _puddle.TrySpillAt(item, new Solution(reagentProto, reagentQuantity), out var _, false);
    }
}
