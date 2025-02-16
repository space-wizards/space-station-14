using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Shuttles.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Survivor;
using Content.Shared.Survivor.Components;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddSurvivorRoleEvent>(OnAddSurvivorRole);
        SubscribeLocalEvent<SurvivorRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnAddSurvivorRole(ref AddSurvivorRoleEvent args)
    {
        const string survivorRule = "Survivor";

        if (!_mind.TryGetMind(args.ToBeSurvivor, out var mind, out var mindComp) || HasComp<SurvivorComponent>(args.ToBeSurvivor))
            return;

        EnsureComp<SurvivorComponent>(args.ToBeSurvivor);
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(args.ToBeSurvivor)} has become a Survivor!");
        _role.MindAddRole(mind, "MindRoleSurvivor");
        _antag.SendBriefing(args.ToBeSurvivor, Loc.GetString("survivor-role-greeting"), Color.Olive, null);

        if (!GameTicker.IsGameRuleActive<SurvivorRuleComponent>())
            GameTicker.StartGameRule(survivorRule);
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
        var existingSurvivors = EntityQuery<SurvivorComponent, TransformComponent, MobStateComponent>();

        var alive = 0;
        var aliveOnShuttle = 0;
        var eShuttle = _eShuttle.GetShuttle();

        if (eShuttle is null)
            return;

        var eShuttleMapPos = _xform.GetMapCoordinates(Transform(eShuttle.Value));

        foreach (var (_, xform, mobStateComp) in existingSurvivors)
        {
            // Checking this instead of the system since we're already going through a query
            //  Can't get .Owner so there'd have to be an entire different way to get a UID
            //   Which is a lot more messy than just doing this
            if (mobStateComp.CurrentState != MobState.Alive)
                continue;

            if (eShuttleMapPos.MapId != _xform.GetMapCoordinates(xform).MapId)
            {
                alive++;
                continue;
            }

            aliveOnShuttle++;
        }

        args.AddLine(Loc.GetString("survivor-round-end-alive-count", ("aliveCount", alive)));
        args.AddLine(Loc.GetString("survivor-round-end-alive-on-shuttle-count", ("aliveCount", aliveOnShuttle)));

        // Player manifest at EOR shows who's a survivor so no need for extra info here.
    }
}
