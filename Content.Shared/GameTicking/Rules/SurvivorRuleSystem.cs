using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Survivor.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameTicking.Rules;

public sealed partial class SurvivorRuleSystem : GameRuleSystem<SurvivorRuleComponent>
{
    [Dependency] private AliveHumanoidTargetSystem _target = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedEmergencyShuttleSystem _eShuttle = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> InvalidForSurvivorAntagTag = "InvalidForSurvivorAntag";

    // TODO: Planned rework post wizard release when RandomGlobalSpawnSpell becomes a gamerule
    protected override void Started(Entity<SurvivorRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        var allAliveHumanMinds = _target.GetMinds();

        foreach (var humanMind in allAliveHumanMinds)
        {
            if (!humanMind.Comp.OwnedEntity.HasValue)
                continue;

            var mind = humanMind.Owner;
            var ent = humanMind.Comp.OwnedEntity.Value;

            if (HasComp<SurvivorComponent>(mind) || _tag.HasTag(mind, InvalidForSurvivorAntagTag))
                continue;

            EnsureComp<SurvivorComponent>(mind);
            _role.MindAddRole(mind, "MindRoleSurvivor");
            _antag.SendBriefing(ent, Loc.GetString("survivor-role-greeting"), Color.Olive, null);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<SurvivorRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("survivor-role-greeting"));
    }

    protected override void AppendRoundEndText(Entity<SurvivorRuleComponent> rule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(rule, ref args);

        // Using this instead of alive antagonists to make checking for shuttle & if the ent is alive easier
        var existingSurvivors = AllEntityQuery<SurvivorComponent, MindComponent>();

        var deadSurvivors = 0;
        var aliveMarooned = 0;
        var aliveOnShuttle = 0;
        var eShuttle = _eShuttle.GetShuttle();

        while (existingSurvivors.MoveNext(out _, out _, out var mindComp))
        {
            // If their brain is gone or they respawned/became a ghost role
            if (mindComp.CurrentEntity is null)
            {
                deadSurvivors++;
                continue;
            }

            var survivor = mindComp.CurrentEntity.Value;

            if (!_mobState.IsAlive(survivor))
            {
                deadSurvivors++;
                continue;
            }

            if (eShuttle != null && eShuttle.Value.IsValid() && (Transform(eShuttle.Value).MapID == _xform.GetMapCoordinates(survivor).MapId))
            {
                aliveOnShuttle++;
                continue;
            }

            aliveMarooned++;
        }

        args.AddLine(Loc.GetString("survivor-round-end-dead-count", ("deadCount", deadSurvivors)));
        args.AddLine(Loc.GetString("survivor-round-end-alive-count", ("aliveCount", aliveMarooned)));
        args.AddLine(Loc.GetString("survivor-round-end-alive-on-shuttle-count", ("aliveCount", aliveOnShuttle)));
        args.AddLine("");

        // Player manifest at EOR shows who's a survivor so no need for extra info here.
    }
}
