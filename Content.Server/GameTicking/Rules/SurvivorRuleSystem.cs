using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Survivor;
using Content.Shared.Survivor.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class SurvivorRuleSystem : GameRuleSystem<SurvivorRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddSurvivorRoleEvent>(OnAddSurvivorRole);
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

    protected override void AppendRoundEndText(EntityUid uid,
        SurvivorRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        // Using this instead of alive antagonists to make checking for centcomm easier
        var aliveSurvivors = EntityQuery<SurvivorComponent, TransformComponent>();

        args.AddLine(Loc.GetString("survivor-round-end-alive-count", ("aliveCount", aliveSurvivors.Count())));

        var aliveOnCentComm = 0;
        var centCommMapUid = _roundEnd.GetCentcomm();

        if (centCommMapUid is null)
            return;

        // TODO: Detects CentComm but didn't detect the survivor ON centcomm
        foreach (var (_, xform) in aliveSurvivors)
        {
            var mapUid = xform.MapUid;

            if (mapUid != centCommMapUid.Value)
                continue;

            aliveOnCentComm++;
        }

        args.AddLine(Loc.GetString("survivor-round-end-alive-on-centcomm-count", ("aliveCount", aliveOnCentComm)));

        // Player manifest at EOR shows who's a survivor so no need for extra info here.
    }
}
