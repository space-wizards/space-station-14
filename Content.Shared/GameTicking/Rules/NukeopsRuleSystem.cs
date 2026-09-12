using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.GameTicking.Rules;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class NukeopsRuleSystem : GameRuleSystem<NukeopsRuleComponent>
{
    [Dependency] protected AntagSelectionSystem Antag = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;

    // TODO: This shouldn't be matching by ProtoId.
    // It would be better if this were checked by component or something,
    // but it needs to be distinct between the full Nukeops and Loneops rules,
    // which NukeopsRuleComponent currently isn't.
    // Better yet, maybe the behaviors this is used for could be moved to the rule component.
    public static readonly EntProtoId NukeopsGameRule = "Nukeops";

    protected override void Started(Entity<NukeopsRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(rule.Comp1.Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        rule.Comp1.TargetStation = RobustRandom.Pick(eligible);
        var ev = new NukeopsTargetStationSelectedEvent(rule, rule.Comp1.TargetStation);
        RaiseLocalEvent(ref ev);
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<NukeopsRoleComponent> role, ref GetBriefingEvent args)
    {
        // TODO Different character screen briefing for the 3 nukie types
        args.Append(Loc.GetString("nukeops-briefing"));
    }

    [SubscribeLocalEvent]
    private void OnAfterAntagEntSelected(Entity<NukeopsRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var target = (ent.Comp.TargetStation is not null) ? Name(ent.Comp.TargetStation.Value) : "the target";

        Antag.SendBriefing(args.Session,
            Loc.GetString("nukeops-welcome",
                ("station", target),
                ("name", Name(ent))),
            Color.Red,
            ent.Comp.GreetSoundNotification);
    }
}


/// <summary>
/// Raised when a station has been assigned as a target for the NukeOps rule.
/// </summary>
[ByRefEvent]
public readonly struct NukeopsTargetStationSelectedEvent(EntityUid ruleEntity, EntityUid? targetStation)
{
    /// <summary>
    /// The entity containing the NukeOps gamerule.
    /// </summary>
    public readonly EntityUid RuleEntity = ruleEntity;

    /// <summary>
    /// The target station, if it exists.
    /// </summary>
    public readonly EntityUid? TargetStation = targetStation;
}

