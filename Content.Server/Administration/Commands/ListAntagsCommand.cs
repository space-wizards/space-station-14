using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListAntagsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedObjectivesSystem _sharedObjectivesSystem = default!;
    [Dependency] private readonly SharedRoleSystem _sharedRoleSystem = default!;

    public override string Command => "lsantags";
    public override string Description => Loc.GetString("lsantags-command-description");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var showObjectives = args.Length > 0 && args[0].ToLower() == "true";
        var antagList = new List<string>();
        var allMinds = _entityManager.EntityQueryEnumerator<MindComponent>();

        while (allMinds.MoveNext(out var mindId, out var mind))
        {
            if (!_sharedRoleSystem.MindIsAntagonist(mindId))
                continue;

            var playerName = mind.Session?.Name ?? Loc.GetString("lsantags-command-unknown");
            var icName = mind.CharacterName ?? Loc.GetString("lsantags-command-nameless");
            var antagRoles = _sharedRoleSystem.MindGetAllRoleInfo(mindId)
                .Where(role => role.Antagonist)
                .Select(role => Loc.GetString(role.Name))
                .ToList();
            var antagRolesStr = string.Join(", ", antagRoles);
            var entityInfo = _entityManager.ToPrettyString(mindId);

            var antagInfo = Loc.GetString(
                "lsantags-command-list-info",
                ("playerName", playerName),
                ("entityInfo", entityInfo),
                ("antagRoles", antagRolesStr)
            );

            if (showObjectives)
            {
                var objectives = mind.Objectives.ToList();
                if (objectives.Count > 0)
                {
                    antagInfo += "\n  Objectives:";
                    foreach (var obj in objectives)
                    {
                        var info = _sharedObjectivesSystem.GetInfo(obj, mindId, mind);
                        if (info != null)
                        {
                            var progress = (int)(info.Value.Progress * 100f);
                            antagInfo += $"\n  - {info.Value.Title} ({progress}%)";
                        }
                        else
                        {
                            antagInfo += "\n  - INVALID OBJECTIVE";
                        }
                    }
                }
                else
                {
                    antagInfo += "\n  No objectives.";
                }
            }

            antagList.Add(antagInfo);
        }

        if (antagList.Count == 0)
        {
            shell.WriteLine(Loc.GetString("lsantags-command-no-antags"));
            return;
        }

        shell.WriteLine($"{Loc.GetString("lsantags-command-list-antags")}\n{string.Join("\n", antagList)}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var option = new CompletionOption[]
            {
                new("true", Loc.GetString("lsantags-command-auto-completion-true")),
                new("false", Loc.GetString("lsantags-command-auto-completion-false"))
            };

            return CompletionResult.FromHintOptions(option, Loc.GetString("lsantags-command-auto-completion"));
        }

        return CompletionResult.Empty;
    }
}
