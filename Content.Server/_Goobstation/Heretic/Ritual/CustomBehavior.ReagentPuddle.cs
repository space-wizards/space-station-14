using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids.Components;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualReagentPuddleBehavior : RitualCustomBehavior
{
    protected EntityLookupSystem _lookup = default!;

    [DataField] public List<ProtoId<ReagentPrototype>>? Reagents;

    private List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        if (Reagents == null)
        {
            //should only happen if someone fucked up their ritual yaml
            outstr = Loc.GetString("heretic-ritual-unknown");
            return false;
        }
        string reagStrings = "";

        foreach(var Reagent in Reagents)
        {
            reagStrings += (Reagent.Id + ", ");

            outstr = null;
            _lookup = args.EntityManager.System<EntityLookupSystem>();

            var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);

            foreach (var ent in lookup)
            {
                if (!args.EntityManager.TryGetComponent<PuddleComponent>(ent, out var puddle))
                    continue;

                if (puddle.Solution == null)
                    continue;

                var soln = puddle.Solution.Value;

                if (!soln.Comp.Solution.ContainsPrototype(Reagent))
                    continue;

                uids.Add(ent);
            }

            if (uids.Count == 0)
            {
                continue;
            }

            return true;
        }

        //take off the comma + space on the end of the reagStrings
        reagStrings = reagStrings.Substring(0, reagStrings.Length - 2);
        outstr = Loc.GetString("heretic-ritual-fail-reagentpuddle", ("reagentname", reagStrings));
        return false;

    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in uids)
            args.EntityManager.QueueDeleteEntity(uid);
        uids = new();
    }
}
