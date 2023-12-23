using Content.Client.Actions;
using Content.Client.Mapping;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

// Disabled until sandoxing issues are resolved. In the meantime, if you want to create an acttions preset, just disable
// sandboxing and uncomment this code (and the SaveActionAssignments() function).
/*
[AnyCommand]
public sealed class SaveActionsCommand : IConsoleCommand
{
    public string Command => "saveacts";

    public string Description => "Saves the current action toolbar assignments to a file";

    public string Help => $"Usage: {Command} <user resource path>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        try
        {
            EntitySystem.Get<ActionsSystem>().SaveActionAssignments(args[0]);
        }
        catch
        {
            shell.WriteLine("Failed to save action assignments");
        }
    }
}
*/

[AnyCommand]
public sealed class LoadActionsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "loadacts";
    public string Description => Loc.GetString("actions-command-load-description");
    public string Help => Loc.GetString("actions-command-load-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        try
        {
            _entitySystemManager.GetEntitySystem<ActionsSystem>().LoadActionAssignments(args[0], true);
        }
        catch
        {
            shell.WriteError(Loc.GetString("actions-command-load-error"));
        }
    }
}

[AnyCommand]
public sealed class LoadMappingActionsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once StringLiteralTypo
    public const string CommandName = "loadmapacts";
    public string Command => CommandName;
    public string Description => Loc.GetString("actions-command-load-mapping-description");
    public string Help => Loc.GetString("actions-command-load-mapping-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            _entitySystemManager.GetEntitySystem<MappingSystem>().LoadMappingActions();
        }
        catch
        {
            shell.WriteError(Loc.GetString("actions-command-load-mapping-error"));
        }
    }
}
