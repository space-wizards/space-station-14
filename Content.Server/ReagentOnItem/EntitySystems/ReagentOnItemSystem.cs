using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;

namespace Content.Server.ReagentOnItem;

/// <summary>
///     Deals with items that apply a solution on use to other entities.
/// </summary>
public sealed class ReagentOnItemSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    ///     Tries to apply the given solution to the item.
    ///     It will apply all reagents that can stick and will spill anything
    ///     that cannot stick to the floor.
    /// </summary>
    /// <returns> False if there is no solution or if entity isn't an item and true otherwise. </returns>
    public bool AddReagentToItem(EntityUid item, Solution reagentMixture)
    {
        if (reagentMixture.Volume <= 0 || !HasComp<ItemComponent>(item))
        {
            return false;
        }

        // Make sure the item doesn't have a non stick surface (E.g jani gloves or boots)
        if (!HasComp<NonStickSurfaceComponent>(item))
        {
            // Yes this code is sussy but its much better than what was used before.
            // All suspect code for this system is contained here (I hope).

            // TODO: Replace this with something more modular.

            // Remove all reagents that can actually stick to things and apply them
            // to the item.
            var volSpaceLube = reagentMixture.RemoveReagent("SpaceLube", reagentMixture.Volume).Double();
            var volSpaceGlue = reagentMixture.RemoveReagent("SpaceGlue", reagentMixture.Volume).Double();

            // var test = reagentMixture.TryGetReagent("SpaceGlue", out var goodout);
            if (volSpaceLube > 0)
            {
                var lubed = EnsureComp<SpaceLubeOnItemComponent>(item);
                ConvertReagentToStacks(lubed, "SpaceLube", volSpaceLube, reagentMixture);
            }
            if (volSpaceGlue > 0)
            {
                var glued = EnsureComp<SpaceGlueOnItemComponent>(item);
                ConvertReagentToStacks(glued, "SpaceGlue", volSpaceGlue, reagentMixture);
            }
        }
        else
        {
            // This means the item had a nonstick surface.
            _popup.PopupEntity(Loc.GetString("non-stick-gloves-reagent-falls-off", ("target", Identity.Entity(item, EntityManager))), item);
        }

        // This spills the remaining mixture that wasn't applied.
        _puddle.TrySpillAt(item, reagentMixture, out var puddle, false);

        return true;

    }
    /// <summary>
    ///     Convert the reagent to stacks and add them to the component. 
    ///     Will put any extra reagent that couldn't be applied in the spill pool.
    /// </summary>
    private static void ConvertReagentToStacks(ReagentOnItemComponent comp, string reagentName, double volToAdd, Solution spillPool)
    {
        // This is the TOTAL amount of reagent on the item, old amount + stuff we are adding now
        var total = comp.EffectStacks + volToAdd;

        // If the total is > capacity it will just put the maximum amount on the item.
        // Ohterwise, its just the total amount!
        comp.EffectStacks = Math.Min(total, comp.MaxStacks);

        // This means there is going to be extra reagents we need to add to the
        // puddle.
        if (total > comp.MaxStacks)
        {
            spillPool.AddReagent(reagentName, total - comp.MaxStacks);
        }
    }

}
