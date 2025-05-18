using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Shuttles.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Survivor.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class SurvivorRuleSystem : GameRuleSystem<SurvivorRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly EmergencyShuttleSystem _eShuttle = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly ProtoId<TagPrototype> InvalidForSurvivorAntagTag = "InvalidForSurvivorAntag";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurvivorRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // TODO: Planned rework post wizard release when RandomGlobalSpawnSpell becomes a gamerule
    protected override void Started(EntityUid uid, SurvivorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var allAliveHumanMinds = _mind.GetAliveHumans();

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

    private void OnGetBriefing(Entity<SurvivorRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("survivor-role-greeting"));
    }

    protected override void AppendRoundEndText(EntityUid uid,
        SurvivorRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

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

        // Player manifest at EOR shows who's a survivor so no need for extra info here.
    }
}
