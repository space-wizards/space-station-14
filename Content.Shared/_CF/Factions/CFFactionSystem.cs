using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Factions;

/// <summary>
/// Party members cannot damage each other. Opposition is a valid target. Neutrals do not hunt other neutrals.
/// </summary>
public sealed partial class CFFactionSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private readonly Dictionary<string, string> _jobFactions = new();
    private readonly List<EntityUid> _hostileRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        RebuildJobMap();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<CFFactionPrototype>())
            RebuildJobMap();
    }

    private void RebuildJobMap()
    {
        _jobFactions.Clear();
        foreach (var faction in _prototypes.EnumeratePrototypes<CFFactionPrototype>())
        {
            foreach (var job in faction.Jobs)
                _jobFactions[job.Id] = faction.ID;
        }
    }

    [SubscribeLocalEvent]
    private void OnGettingAttacked(Entity<CFFactionComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        if (args.Cancelled || args.Disarm)
            return;

        if (CanHarm(args.Attacker, ent.Owner))
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CFFactionComponent> ent, ref ExaminedEvent args)
    {
        if (!_prototypes.TryIndex(ent.Comp.Faction, out CFFactionPrototype? faction))
            return;

        args.PushMarkup(Loc.GetString("cf-faction-examine", ("faction", Loc.GetString(faction.Name))));
    }

    public void AssignJob(EntityUid mob, string? jobId)
    {
        var faction = jobId != null && _jobFactions.TryGetValue(jobId, out var id)
            ? id
            : "Neutral";
        SetFaction(mob, faction);
    }

    public void SetFaction(EntityUid mob, ProtoId<CFFactionPrototype> faction)
    {
        var comp = EnsureComp<CFFactionComponent>(mob);
        if (comp.Faction == faction)
            return;

        comp.Faction = faction;
        Dirty(mob, comp);
    }

    /// <summary>
    /// False only when both creatures have factions and their alliance is <see cref="CFAlliance.Party"/>.
    /// </summary>
    public bool CanHarm(EntityUid attacker, EntityUid target)
    {
        if (attacker == target)
            return false;

        if (!HasComp<CFFactionComponent>(attacker) || !HasComp<CFFactionComponent>(target))
            return true;

        return GetAlliance(attacker, target) != CFAlliance.Party;
    }

    public CFAlliance GetAlliance(EntityUid a, EntityUid b)
    {
        if (!TryComp(a, out CFFactionComponent? factionA) || !TryComp(b, out CFFactionComponent? factionB))
            return CFAlliance.Neutral;

        if (!_prototypes.TryIndex(factionA.Faction, out CFFactionPrototype? protoA)
            || !_prototypes.TryIndex(factionB.Faction, out CFFactionPrototype? protoB))
            return CFAlliance.Neutral;

        if (factionA.Faction == factionB.Faction)
            return protoA.Same;

        if (protoA.Party.Contains(factionB.Faction) || protoB.Party.Contains(factionA.Faction))
            return CFAlliance.Party;

        if (protoA.Opposition.Contains(factionB.Faction) || protoB.Opposition.Contains(factionA.Faction))
            return CFAlliance.Opposition;

        return CFAlliance.Neutral;
    }

    /// <summary>
    /// Drops party and neutral CF creatures from an NPC hostile query and adds opposition targets.
    /// Mobs with no CF faction keep the stock faction list.
    /// </summary>
    public void ApplyHostileQuery(EntityUid owner, float range, HashSet<EntityUid> entities)
    {
        if (!TryComp(owner, out CFFactionComponent? faction))
            return;

        _hostileRemove.Clear();
        foreach (var target in entities)
        {
            if (!HasComp<CFFactionComponent>(target))
                continue;

            if (GetAlliance(owner, target) != CFAlliance.Opposition)
                _hostileRemove.Add(target);
        }

        foreach (var target in _hostileRemove)
            entities.Remove(target);

        if (!_prototypes.TryIndex(faction.Faction, out CFFactionPrototype? proto) || proto.Opposition.Count == 0)
            return;

        foreach (var other in _lookup.GetEntitiesInRange<MobStateComponent>(Transform(owner).Coordinates, range))
        {
            if (other.Owner == owner)
                continue;

            if (TryComp(other.Owner, out MobStateComponent? state) && state.CurrentState == MobState.Dead)
                continue;

            if (GetAlliance(owner, other.Owner) == CFAlliance.Opposition)
                entities.Add(other.Owner);
        }
    }
}
