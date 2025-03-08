using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListAntagsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedObjectivesSystem _sharedObjectivesSystem = default!;
    [Dependency] private readonly SharedRoleSystem _sharedRoleSystem = default!;

    public override string Command => "lsantags";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var showObjectives = args.Length > 0 && args[0].ToLower() == "true";
        var antagList = new List<string>();
        var allMinds = _entityManager.EntityQueryEnumerator<MindComponent>();

        while (allMinds.MoveNext(out var mindId, out var mind))
        {
            if (!_sharedRoleSystem.MindIsAntagonist(mindId))
                continue;

            var playerName = mind.Session?.Name ?? Loc.GetString("cmd-lsantags-unknown");
            var icName = mind.CharacterName ?? Loc.GetString("cmd-lsantags-nameless");
            var antagRoles = _sharedRoleSystem.MindGetAllRoleInfo(mindId)
                .Where(role => role.Antagonist)
                .Select(role => Loc.GetString(role.Name))
                .ToList();
            var antagRolesStr = string.Join(", ", antagRoles);
            var entityInfo = _entityManager.ToPrettyString(mindId);

            var antagInfo = Loc.GetString(
                "cmd-lsantags-list-info",
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
                            antagInfo += $"\n  - {Loc.GetString("cmd-lsantags-invalid-objecive")}";
                        }
                    }
                }
                else
                {
                    antagInfo += $"\n  {Loc.GetString("cmd-lsantags-no-objectives")}";
                }
            }

            antagList.Add(antagInfo);
        }

        if (antagList.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-lsantags-no-antags"));
            return;
        }

        shell.WriteLine($"{Loc.GetString("cmd-lsantags-list-antags")}\n{string.Join("\n", antagList)}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var option = new CompletionOption[]
            {
                new("true", Loc.GetString("cmd-lsantags-auto-completion-true")),
                new("false", Loc.GetString("cmd-lsantags-auto-completion-false"))
            };

            return CompletionResult.FromHintOptions(option, Loc.GetString("cmd-lsantags-auto-completion"));
        }

        return CompletionResult.Empty;
    }
}
