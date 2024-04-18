// Only has one function: Add reagent to item.
// Takes the reagent and the item and puts the correc
// reagent on it and spills the rest.

// Should be "imported" to functions that need it 

// Also deal with gloves here becasuse I can't see a situation where
// it should ignore them. Can be refactored later if for some reason
// it needs to be changed

using Content.Shared.Chemistry.Components;
using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Glue;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Shared.ReagentOnItem;
using Robust.Shared.Toolshed.Commands.Math;
using System.Diagnostics;
using Content.Shared.FixedPoint;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;

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

    public bool AddReagentToItem(EntityUid item, Solution reagentMixture)
    {
        if (reagentMixture.Volume <= 0)
        {
            return true;
        }

        if (HasComp<ItemComponent>(item))
        {
            if (!HasComp<NonStickSurfaceComponent>(item))
            {
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

            if (HasComp<NonStickSurfaceComponent>(item))
            {
                // The liquid slips right off the [ITEM]!
                _popup.PopupEntity("The liquid slips right off!", item);
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
