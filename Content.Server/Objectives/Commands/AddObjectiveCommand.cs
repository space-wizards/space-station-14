using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddObjectiveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override string Command => "addobjective";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString(Loc.GetString("cmd-addobjective-invalid-args")));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var data))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-player-not-found"));
            return;
        }

        if (!_mind.TryGetMind(data, out var mindId, out var mind))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-mind-not-found"));
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(args[1], out var proto) ||
            !proto.HasComponent<ObjectiveComponent>())
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-objective-not-found", ("obj", args[1])));
            return;
        }

        if (!_mind.TryAddObjective(mindId, mind, args[1]))
        {
            // can fail for other reasons so dont pretend to be right
            shell.WriteError(Loc.GetString("cmd-addobjective-adding-failed"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-addobjective-player-completion"));
        }

        if (args.Length != 2)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(
            _objectives.Objectives(),
            Loc.GetString(Loc.GetString("cmd-add-objective-obj-completion")));
    }
}
