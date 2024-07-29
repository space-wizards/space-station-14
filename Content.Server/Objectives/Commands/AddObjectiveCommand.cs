using System.Linq;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddObjectiveCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override string Command => "addobjective";

    private IEnumerable<string>? _objectives = null;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString(Loc.GetString("cmd-addobjective-invalid-args")));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var data))
        {
            shell.WriteLine(Loc.GetString("cmd-addobjective-player-not-found"));
            return;
        }

        var minds = _entityManager.System<SharedMindSystem>();
        if (!minds.TryGetMind(data, out var mindId, out var mind))
        {
            shell.WriteLine(Loc.GetString("cmd-addobjective-mind-not-found"));
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(args[1], out var proto) ||
            !proto.HasComponent<ObjectiveComponent>())
        {
            shell.WriteLine(Loc.GetString("cmd-addobjectives-objective-not-found", ("obj", args[1])));
            return;
        }

        if (!minds.TryAddObjective(mindId, mind, args[1]))
        {
            // can fail for other reasons so dont pretend to be right
            shell.WriteLine(Loc.GetString("cmd-addobjective-adding-failed"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, "<Player>");
        }

        if (args.Length != 2)
            return CompletionResult.Empty;

        if (_objectives == null)
        {
            CreateCompletions();
            _prototypes.PrototypesReloaded += _ => CreateCompletions();
        }

        return CompletionResult.FromHintOptions(
            _objectives!,
            "<Objective>");
    }

    private void CreateCompletions()
    {
        _objectives = _prototypes.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.HasComponent<ObjectiveComponent>())
            .Select(p => p.ID)
            .Order();
    }
}
