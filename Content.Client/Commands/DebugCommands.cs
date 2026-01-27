using Content.Client.Markers;
using Content.Client.Popups;
using Content.Client.SubFloor;
using Robust.Shared.Console;

namespace Content.Client.Commands;

internal sealed class ShowMarkersCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MarkerSystem _markerSystem = default!;

    public override string Command => "showmarkers";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _markerSystem.MarkersVisible ^= true;
    }
}

internal sealed class ShowSubFloor : LocalizedEntityCommands
{
    [Dependency] private readonly SubFloorHideSystem _subfloorSystem = default!;

    public override string Command => "showsubfloor";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _subfloorSystem.ShowAll ^= true;
    }
}

internal sealed class NotifyCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override string Command => "notify";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _popupSystem.PopupCursor(args[0]);
    }
}
