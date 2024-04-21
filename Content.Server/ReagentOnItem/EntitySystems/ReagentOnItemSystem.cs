using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;

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

        if (!HasComp<NonStickSurfaceComponent>(item))
        {
            // Yes this code is sussy but its much better than what was used before.
            // All suspect code for this system is contained here (I hope).

            // TODO: Replace this with something more modular.

            // Remove all reagents that can actually stick to things and apply them
            // to the item.
            var volSpaceLube = reagentMixture.RemoveReagent("SpaceLube", reagentMixture.Volume);
            var volSpaceGlue = reagentMixture.RemoveReagent("SpaceGlue", reagentMixture.Volume);

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
            _popup.PopupEntity(Loc.GetString("non-stick-gloves-reagent-falls-off", ("target", Identity.Entity(item, EntityManager))), item);
        }

        _puddle.TrySpillAt(item, reagentMixture, out var puddle, false);

        return true;

    }
    /// <summary>
    ///     Convert the reagent to stacks and add them to the component. 
    ///     Will put any extra reagent that couldn't be applied in the spill pool.
    /// </summary>
    private static void ConvertReagentToStacks(ReagentOnItemComponent comp, string reagentName, FixedPoint2 volToAdd, Solution spillPool)
    {
        var total = comp.EffectStacks + volToAdd;

        comp.EffectStacks = FixedPoint2.Min(total, comp.MaxStacks);

        if (total > comp.MaxStacks)
        {
            spillPool.AddReagent(reagentName, total - comp.MaxStacks);
        }
    }

}
