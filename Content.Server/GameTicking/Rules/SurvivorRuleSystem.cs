using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Shuttles.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Survivor.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;

namespace Content.Server.GameTicking.Rules;

public sealed class SurvivorRuleSystem : GameRuleSystem<SurvivorRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly EmergencyShuttleSystem _eShuttle = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurvivorRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // TODO: Planned rework post wizard release when RandomGlobalSpawnSpell becomes a gamerule
    protected override void Started(EntityUid uid, SurvivorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var allHumans = _mind.GetAliveHumans();

        foreach (var human in allHumans)
        {
            if (!human.Comp.OwnedEntity.HasValue)
                continue;

            var ent = human.Comp.OwnedEntity.Value;

            if (_tag.HasTag(ent, "InvalidForSurvivorAntag"))
                continue;

            // Alive Humans does get the mindcomp, however this has better logic to get the mindId
            // No need to repeat the same code
            if (!_mind.TryGetMind(ent, out var mind, out _) || HasComp<SurvivorComponent>(ent))
                continue;

            EnsureComp<SurvivorComponent>(ent);
            _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ent)} has become a Survivor!");
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
        var existingSurvivors = AllEntityQuery<SurvivorComponent, TransformComponent, MobStateComponent>();

        var aliveMarooned = 0;
        var aliveOnShuttle = 0;
        var eShuttle = _eShuttle.GetShuttle();

        if (eShuttle is null)
            return;

        if (!eShuttle.Value.IsValid())
            return;

        var eShuttleMapPos = _xform.GetMapCoordinates(Transform(eShuttle.Value));

        while (existingSurvivors.MoveNext(out var survivor, out _, out var xform, out var mobState))
        {
            if (!_mobState.IsAlive(survivor, mobState))
                continue;

            if (eShuttleMapPos.MapId != _xform.GetMapCoordinates(survivor, xform).MapId)
            {
                aliveMarooned++;
                continue;
            }

            aliveOnShuttle++;
        }

        args.AddLine(Loc.GetString("survivor-round-end-alive-count", ("aliveCount", aliveMarooned)));
        args.AddLine(Loc.GetString("survivor-round-end-alive-on-shuttle-count", ("aliveCount", aliveOnShuttle)));

        // Player manifest at EOR shows who's a survivor so no need for extra info here.
    }
}
