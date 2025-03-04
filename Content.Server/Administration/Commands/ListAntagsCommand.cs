using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;


namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListAntagsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AdminSystem _adminSystem = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override string Command => "lsantags";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var antagList = new List<string>();

        // We get all players who have mind
        var allMinds = _entityManager.EntityQueryEnumerator<MindComponent>();

        while (allMinds.MoveNext(out var mindId, out var mind))
        {
            if (!_role.MindIsAntagonist(mindId))
                continue;

            // Getting information about the player
            var playerName = mind.Session?.Name ?? "Неизвестный";
            var icName = mind.CharacterName ?? "Безымянный";
            var antagRoles = _role.MindGetAllRoleInfo(mindId)
                .Where(role => role.Antagonist)
                .Select(role => Loc.GetString(role.Name))
                .ToList();

            var antagRolesStr = string.Join(", ", antagRoles);
            var entityInfo = _entityManager.ToPrettyString(mindId);

            antagList.Add($"{playerName} {entityInfo} - Roles: {Loc.GetString(antagRolesStr)}");
        }

        if (antagList.Count == 0)
        {
            shell.WriteLine("Антагонистов нет.");
        }
        else
        {
            shell.WriteLine("Список антагонистов:\n" + string.Join("\n", antagList));
        }
    }

}
