using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared.ReagentOnItem;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;

namespace Content.Server.ReagentOnItem;

public sealed class ReagentOnItemSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    // Try to apply the given solution to an item.
    public bool AddReagentToItem(EntityUid item, Solution reagentMixture)
    {
        if (reagentMixture.Volume <= 0)
        {
            return false;
        }

        if (HasComp<ItemComponent>(item))
        {
            if (!HasComp<NonStickSurfaceComponent>(item))
            {
                // Yes this code is sussy but its much better than what was used before.
                // All suspect code for this system is contained here (I hope).
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

            // Reagents can't stick on certain items. E.g janis gloves and boots.
            if (HasComp<NonStickSurfaceComponent>(item))
            {
                _popup.PopupEntity(Loc.GetString("non-stick-gloves-reagent-falls-off", ("target", Identity.Entity(item, EntityManager))), item);
            }

            var makeNoise = _random.NextDouble() > .75;

            _puddle.TrySpillAt(item, reagentMixture, out var puddle, makeNoise);

            return true;
        }

        return false;
    }

    private static void AddAndDetermineExtra(ReagentOnItemComponent comp, string reagentName, double volToAdd, Solution spillPool)
    {
        var total = comp.AmountOfReagentLeft + volToAdd;
        comp.AmountOfReagentLeft = Math.Min(total, comp.ReagentCapacity);
        if (comp.AmountOfReagentLeft == comp.ReagentCapacity)
        {
            spillPool.AddReagent(reagentName, total - comp.ReagentCapacity);
        }
    }

}
