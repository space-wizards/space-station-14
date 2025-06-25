using Content.Client.Actions;
using Content.Client.Markers;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

internal sealed class MappingClientSideSetupCommand : LocalizedCommands
{
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly ActionsSystem _actionSystem = default!;
    [Dependency] private readonly MarkerSystem _markerSystem = default!;

    public override string Command => "mappingclientsidesetup";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_lightManager.LockConsoleAccess)
            return;

        _markerSystem.MarkersVisible = true;
        _lightManager.Enabled = false;
        shell.ExecuteCommand("showsubfloor"); // boop
        _actionSystem.LoadActionAssignments("/mapping_actions.yml", false);
    }
}

