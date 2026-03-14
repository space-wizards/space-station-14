using Content.Client.Actions;
using Content.Client.Markers;
using Content.Client.SubFloor;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

internal sealed class MappingClientSideSetupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly ActionsSystem _actionSystem = default!;
    [Dependency] private readonly MarkerSystem _markerSystem = default!;
    [Dependency] private readonly SubFloorHideSystem _subfloorSystem = default!;

    public override string Command => "mappingclientsidesetup";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_lightManager.LockConsoleAccess)
            return;

        _markerSystem.MarkersVisible = true;
        _lightManager.Enabled = false;
        _subfloorSystem.ShowAll = true;
        _actionSystem.LoadActionAssignments("/mapping_actions.yml", false);
    }
}

