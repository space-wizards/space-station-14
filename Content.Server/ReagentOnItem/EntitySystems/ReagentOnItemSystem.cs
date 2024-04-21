using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;

namespace Content.Server.ReagentOnItem;

/// <summary>
///     The system that helps deal with a genaric reagent being applied to items.
/// </summary>
public sealed class ReagentOnItemSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    ///     The function tries to apply the given solution mixture to the item.
    ///     It will apply all reagents that can stick and will spill anything
    ///     that cannot stick to the floor.
    /// </summary>
    /// <returns> Returns false if there is no solution or if entity isn't an item and true otherwise. </returns>
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

            // Remove all reagents that can actually stick to things and apply them
            // to the item.
            var volSpaceLube = reagentMixture.RemoveReagent("SpaceLube", reagentMixture.Volume).Double();
            var volSpaceGlue = reagentMixture.RemoveReagent("SpaceGlue", reagentMixture.Volume).Double();

            if (volSpaceLube > 0)
            {
                var lubed = EnsureComp<SpaceLubeOnItemComponent>(item);
                AddAndDetermineExtra(lubed, "SpaceLube", volSpaceLube, reagentMixture);
            }
            if (volSpaceGlue > 0)
            {
                var glued = EnsureComp<SpaceGlueOnItemComponent>(item);
                AddAndDetermineExtra(glued, "SpaceGlue", volSpaceGlue, reagentMixture);
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
    ///     Actually add the (valid) reagent to the item and if there is any extra.
    /// </summary>
    /// <returns> The amount of leftover reagent that could not be applied. </returns>
    private static void AddAndDetermineExtra(ReagentOnItemComponent comp, string reagentName, double volToAdd, Solution spillPool)
    {
        // This is the TOTAL amount of reagent on the item, old amount + stuff we are adding now
        var total = comp.AmountOfReagentLeft + volToAdd;

        // If the total is > capacity it will just put the maximum amount on the item.
        // Ohterwise, its just the total amount!
        comp.AmountOfReagentLeft = Math.Min(total, comp.ReagentCapacity);

        // This means there is going to be extra reagents we need to add to the
        // puddle.
        if (total > comp.ReagentCapacity)
        {
            spillPool.AddReagent(reagentName, total - comp.ReagentCapacity);
        }
    }

}
