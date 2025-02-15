using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Survivor;
using Content.Shared.Survivor.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class SurvivorRuleSystem : GameRuleSystem<SurvivorRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddSurvivorRoleEvent>(OnAddSurvivorRole);
    }

    private void OnAddSurvivorRole(ref AddSurvivorRoleEvent args)
    {
        if (!_mind.TryGetMind(args.ToBeSurvivor, out var mind, out _) || HasComp<SurvivorComponent>(args.ToBeSurvivor))
            return;

        EnsureComp<SurvivorComponent>(args.ToBeSurvivor);
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(args.ToBeSurvivor)} has become a Survivor!");
        _role.MindAddRole(mind, "MindRoleSurvivor");
        _antag.SendBriefing(args.ToBeSurvivor, Loc.GetString("survivor-role-greeting"), Color.Olive, null);
    }

    // TODO: Round End
}
