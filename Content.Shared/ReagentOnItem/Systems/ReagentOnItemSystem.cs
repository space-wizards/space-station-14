using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Shared.Fluids;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;

using Robust.Shared.GameStates;

namespace Content.Shared.ReagentOnItem;

/// <summary>
///     Deals with items that apply a solution on use to other entities.
/// </summary>
public sealed class ReagentOnItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    ///     Tries to apply the given solution to the item.
    ///     It will apply all reagents that can stick and will spill anything
    ///     that cannot stick to the floor.
    /// </summary>
    /// <returns> False if there is no solution or if entity isn't an item and true otherwise. </returns>
    public bool ApplyReagentEffectToItem(EntityUid item, Solution reagentMixture)
    {
        if (reagentMixture.Volume <= 0 || !HasComp<ItemComponent>(item))
            return false;

        if (HasComp<NonStickSurfaceComponent>(item))
        {
            _popup.PopupEntity(Loc.GetString("non-stick-gloves-reagent-falls-off", ("target", Identity.Entity(item, EntityManager))), item);
            _puddle.TrySpillAt(item, reagentMixture, out var _, false);
            return false;
        }

        // Yes this code is sussy but its much better than what was used before.
        // All suspect code for this system is contained here (I hope).

        // TODO: Replace this with something more modular.

        // Remove all reagents that can actually stick to things and apply them
        // to the item.

        var spaceLubeId = new ReagentId("SpaceLube", null);
        var spaceGlueId = new ReagentId("SpaceGlue", null);

        if (reagentMixture.TryGetReagent(spaceLubeId, out var _))
        {
            var volSpaceLube = reagentMixture.RemoveReagent(spaceLubeId, reagentMixture.Volume);
            var lubed = EnsureComp<SpaceLubeOnItemComponent>(item);
            ConvertReagentToStacks(item, lubed, spaceLubeId, volSpaceLube, reagentMixture);
        }

        if (reagentMixture.TryGetReagent(spaceGlueId, out var _))
        {
            var volSpaceGlue = reagentMixture.RemoveReagent(spaceGlueId, reagentMixture.Volume);
            var glued = EnsureComp<SpaceGlueOnItemComponent>(item);
            ConvertReagentToStacks(item, glued, spaceGlueId, volSpaceGlue, reagentMixture);
        }

        _puddle.TrySpillAt(item, reagentMixture, out var _, false);

        return true;
    }

    /// <summary>
    ///     Convert the reagent to stacks and add them to the component. 
    ///     Will put any extra reagent that couldn't be applied in the spill pool.
    /// </summary>
    private void ConvertReagentToStacks(EntityUid item, ReagentOnItemComponent comp, ReagentId reagent, FixedPoint2 volToAdd, Solution spillPool)
    {
        var total = comp.EffectStacks + volToAdd;

        comp.EffectStacks = FixedPoint2.Min(total, comp.MaxStacks);

        Dirty(item, comp);

        if (total > comp.MaxStacks)
        {
            spillPool.AddReagent(reagent, total - comp.MaxStacks);
        }
    }
}
