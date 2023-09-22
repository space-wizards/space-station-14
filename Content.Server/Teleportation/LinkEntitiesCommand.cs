using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Console;

namespace Content.Server._Stalker.Teleportation;

[AdminCommand(AdminFlags.Admin)]
public sealed class LinkEntitiesCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    public string Command => "linkentities";
    public string Description => "Adds specified entities a LinkedEntityComponent and set association between them";
    public string Help => $"Usage: {Command} <entityUid> <entityUid> [deleteEntity]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var firstUid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var secondUid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[1])));
            return;
        }

        var shouldDelete = false;
        if (args.Length > 2 && !bool.TryParse(args[2], out shouldDelete))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[2])));
            return;
        }

        var linkSystem = _systemManager.GetEntitySystem<LinkedEntitySystem>();
        if (linkSystem.TryLink(firstUid, secondUid, shouldDelete))
        {
            shell.WriteLine("Successfully linked");
        }
        else
        {
            shell.WriteLine("Failed to link");
        }
    }
}
