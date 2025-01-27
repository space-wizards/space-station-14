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
    protected SharedMindSystem _mind = default!;
    protected HereticSystem _heretic = default!;
    protected DamageableSystem _damage = default!;
    protected EntityLookupSystem _lookup = default!;
    [Dependency] protected IPrototypeManager _proto = default!;

    protected List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        _mind = args.EntityManager.System<SharedMindSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _damage = args.EntityManager.System<DamageableSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _proto = IoCManager.Resolve<IPrototypeManager>();

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
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look) // only humans
            || (OnlyTargets && !hereticComp.SacrificeTargets.Contains(args.EntityManager.GetNetEntity(look)))) // only targets
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

    public override void Finalize(RitualData args)
    {
        for (int i = 0; i < Max; i++)
        {
            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(uids[i]);
            var knowledgeGain = isCommand ? 2f : 1f;

            // YES!!! GIB!!!
            if (args.EntityManager.TryGetComponent<DamageableComponent>(uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>) "Brute";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(uids[i], new DamageSpecifier(dmgtype, 1984f), true);
            }

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
