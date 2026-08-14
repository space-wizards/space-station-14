using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Content.Shared.SpaceArena.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed partial class SpaceArenaCreateCommand : LocalizedEntityCommands
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_create";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_matches.TryCreateMatch(args[0], args[1], out var match))
        {
            shell.WriteError(Loc.GetString("cmd-arena-create-failed", ("mode", args[0]), ("arena", args[1])));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-arena-create-success", ("match", EntityManager.GetNetEntity(match))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var modes = _prototypes.EnumeratePrototypes<EntityPrototype>()
                .Where(prototype => prototype.HasComp<SpaceArenaMatchComponent>(EntityManager.ComponentFactory))
                .Select(prototype => prototype.ID);
            return CompletionResult.FromHintOptions(modes, Loc.GetString("cmd-arena-create-mode-hint"));
        }

        if (args.Length == 2)
        {
            var arenas = _prototypes.EnumeratePrototypes<GameMapPrototype>()
                .Where(prototype => prototype.SpaceArena != null)
                .Select(prototype => prototype.ID);
            return CompletionResult.FromHintOptions(arenas, Loc.GetString("cmd-arena-create-map-hint"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed partial class SpaceArenaJoinCommand : LocalizedEntityCommands
{
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_join";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player == null)
        {
            shell.WriteError(Loc.GetString("cmd-arena-player-required"));
            return;
        }

        if (!TryParseEntity(args[0], out var match) || !_matches.TryJoinMatch(match, shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-arena-join-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-arena-join-success"));
    }

    private bool TryParseEntity(string value, out EntityUid entity)
    {
        entity = EntityUid.Invalid;
        if (!NetEntity.TryParse(value, out var netEntity) ||
            !EntityManager.TryGetEntity(netEntity, out var resolved) ||
            resolved is not { } uid)
        {
            return false;
        }

        entity = uid;
        return true;
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed partial class SpaceArenaStartCommand : LocalizedEntityCommands
{
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_start";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 ||
            !NetEntity.TryParse(args[0], out var netEntity) ||
            !EntityManager.TryGetEntity(netEntity, out var match) ||
            match is not { } matchUid ||
            !_matches.TryStartMatch(matchUid))
        {
            shell.WriteError(Loc.GetString("cmd-arena-start-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-arena-start-success"));
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed partial class SpaceArenaFinishCommand : LocalizedEntityCommands
{
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_finish";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 ||
            !NetEntity.TryParse(args[0], out var netEntity) ||
            !EntityManager.TryGetEntity(netEntity, out var match) ||
            match is not { } matchUid ||
            !_matches.FinishMatch(matchUid))
        {
            shell.WriteError(Loc.GetString("cmd-arena-finish-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-arena-finish-success"));
    }
}

public sealed partial class SpaceArenaLeaveCommand : LocalizedEntityCommands
{
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_leave";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0 || shell.Player == null || !_matches.TryLeaveMatch(shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-arena-leave-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-arena-leave-success"));
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed partial class SpaceArenaListCommand : LocalizedEntityCommands
{
    [Dependency] private SpaceArenaMatchSystem _matches = default!;

    public override string Command => "arena_list";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var matches = _matches.GetMatches();
        if (matches.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-arena-list-empty"));
            return;
        }

        foreach (var match in matches)
        {
            if (!EntityManager.TryGetComponent(match, out SpaceArenaMatchComponent? component))
                continue;

            var mode = EntityManager.GetComponent<MetaDataComponent>(match).EntityPrototype?.ID ?? "unknown";
            shell.WriteLine(Loc.GetString(
                "cmd-arena-list-entry",
                ("match", EntityManager.GetNetEntity(match)),
                ("mode", mode),
                ("state", component.State),
                ("players", component.PlayerCount),
                ("capacity", component.MaxPlayers)));
        }
    }
}
