using Content.Shared.Heretic.Prototypes;
using Content.Shared.Changeling;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Revolutionary.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Humanoid;
using Content.Shared.Forensics.Components;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Server.GameObjects;
using System;
using System.Linq;
using Content.Server._Goobstation.Heretic.EntitySystems;
using Content.Server.Heretic.Components;
using Content.Server.Forensics;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.Forensics;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Content.Shared.Chemistry.EntitySystems;



namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     gibs it and gives the heretic knowledge points.
/// </summary>
// these classes should be lead out and shot
[Virtual] public partial class RitualSacrificeBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField] public float Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField] public float Max = 1;

    /// <summary>
    ///     Should we count only targets?
    /// </summary>
    [DataField] public bool OnlyTargets = false;

    // this is awful but it works so i'm not complaining
    // i'm complaining -kandiyaki
    protected SharedMindSystem _mind = default!;
    protected HereticSystem _heretic = default!;
    protected SharedTransformSystem _xform = default!;
    protected DamageableSystem _damage = default!;
    protected EntityLookupSystem _lookup = default!;
    protected HumanoidAppearanceSystem _humanoid = default!;
    protected TransformSystem _transformSystem = default!;
    protected HellWorldSystem _hellworld = default!;
    protected BloodstreamSystem _bloodstream = default!;
    protected SharedSolutionContainerSystem _solutionContainerSystem = default!;


    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected IEntityManager _entmanager = default!;


    protected List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        //it was like this when i got here -kandiyaki
        _mind = args.EntityManager.System<SharedMindSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _xform = args.EntityManager.System<SharedTransformSystem>();
        _damage = args.EntityManager.System<DamageableSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _humanoid = args.EntityManager.System<HumanoidAppearanceSystem>();
        _transformSystem = args.EntityManager.System<TransformSystem>();
        _hellworld = args.EntityManager.System<HellWorldSystem>();
        _bloodstream = args.EntityManager.System<BloodstreamSystem>();
        _solutionContainerSystem = args.EntityManager.System<SharedSolutionContainerSystem>();

        _proto = IoCManager.Resolve<IPrototypeManager>();
        _entmanager = IoCManager.Resolve<IEntityManager>();


        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);
        if (lookup.Count == 0 || lookup == null)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look)) // only humans
                continue;

            if (mobstate.CurrentState == Shared.Mobs.MobState.Dead)
                uids.Add(look);
        }

        if (uids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    //this does way too much
    public override void Finalize(RitualData args)
    {

        for (int i = 0; i < Max; i++)
        {
            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(uids[i]);
            var knowledgeGain = isCommand ? 2f : 1f;

            //get the humanoid appearance component
            if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(uids[i], out var humanoid))
                return;

            //get the species prototype from that
            if (!_proto.TryIndex(humanoid.Species, out var speciesPrototype))
                return;

            //spawn a clone of the victim
            //this should really use the cloningsystem but i coded this before that existed
            //and it works so i'm not changing it unless it causes issues
            var sacrificialWhiteBoy = args.EntityManager.Spawn(speciesPrototype.Prototype, _transformSystem.GetMapCoordinates(uids[i]));
            _humanoid.CloneAppearance(uids[i], sacrificialWhiteBoy);
            //make sure it has the right DNA
            if (args.EntityManager.TryGetComponent<DnaComponent>(uids[i], out var victimDna))
            {
                if (args.EntityManager.TryGetComponent<BloodstreamComponent>(sacrificialWhiteBoy, out var dummyBlood))
                {
                    //this is copied from BloodstreamSystem's OnDnaGenerated
                    //i hate it
                    if(_solutionContainerSystem.ResolveSolution(sacrificialWhiteBoy, dummyBlood.BloodSolutionName, ref dummyBlood.BloodSolution, out var bloodSolution))
                    {
                        foreach (var reagent in bloodSolution.Contents)
                        {
                            List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                            reagentData.RemoveAll(x => x is DnaData);
                            reagentData.AddRange(_bloodstream.GetEntityBloodData(uids[i]));
                        }
                    }
                }
            }
            //beat the clone to death. this is just to get matching organs
            if (args.EntityManager.TryGetComponent<DamageableComponent>(uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>) "Brute";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(sacrificialWhiteBoy, new DamageSpecifier(dmgtype, 1984f), true);
            }

            //send the target to hell world
            _hellworld.AddVictimComponent(uids[i]);

            //teleport the body to a midround antag spawn spot so it's not just tossed into space
            _hellworld.TeleportRandomly(args, uids[i]);

            //make sure that my shitty AddVictimComponent thing actually worked before trying to use a mind that isn't there
            if (args.EntityManager.TryGetComponent<HellVictimComponent>(uids[i], out var hellVictim))
            {
                //i'm so sorry to all of my computer science professors. i've failed you
                if(hellVictim.HasMind)
                {
                    _hellworld.SendToHell(uids[i], args, speciesPrototype);
                }

            }

            //update the heretic's knowledge
            if (args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
                _heretic.UpdateKnowledge(args.Performer, hereticComp, knowledgeGain);

            // update objectives
            if (_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            {
                // this is godawful dogshit. but it works :)
                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeObjective", out var crewObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                    crewObjComp.Sacrificed += 1;

                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
                && isCommand)
                    crewHeadObjComp.Sacrificed += 1;
            }
        }

        // reset it because it refuses to work otherwise.
        uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }
}
