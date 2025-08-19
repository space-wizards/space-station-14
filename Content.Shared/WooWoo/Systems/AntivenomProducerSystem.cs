using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.WooWoo.Components.Antivenom;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Runtime.InteropServices;

namespace Content.Shared.WooWoo.Systems.Antivenom;

public abstract class SharedAntivenomProducerSystem : EntitySystem
{
    public void AccumulateImmunity(EntityUid ent, AntivenomProducerComponent antivenomComp, ReagentId reagent, FixedPoint2 mostToRemove)
    {

    }

    public void ProduceAntivenom(EntityUid ent, AntivenomProducerComponent antivenomComp, Solution? solution)
    {

    }

    // check if ImmunoCompromised

    // track how much venom has been metabolised and store in MetabolizedTotals on comp
    // when thresholds are reached for venom metab increase the stage and update comp data

    // add antivenom to the chemstream

    // consider whether to increase number of metab'ed reagents so we cant get chock a block with antivenoms

    // put antivenom in bloodstream & also ensure there is room in the bloodstream to add it somehow up to some quantity.
    // also put some in the chemstream to get passive effect from it. This is a dirty hack to account for bloodstream being separaate from chemstream metab. Kill it later.
    // ^ I think syringes solved this already (well its dumb but the curse is delt with there), I can just add it to the chemstream and then when bloodstream is unified we can move it to that.
}
