using Content.Client.Markers;
using Content.Client.Popups;
using Content.Client.SubFloor;
using Robust.Shared.Console;

namespace Content.Client.Commands;

internal sealed partial class ShowMarkersCommand : LocalizedEntityCommands
{
    [Dependency] private MarkerSystem _markerSystem = default!;

    public override string Command => "showmarkers";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _markerSystem.MarkersVisible ^= true;
    }
}

internal sealed partial class ShowSubFloor : LocalizedEntityCommands
{
    [Dependency] private SubFloorHideSystem _subfloorSystem = default!;

    public override string Command => "showsubfloor";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _subfloorSystem.ShowAll ^= true;
    }
}

internal sealed partial class NotifyCommand : LocalizedEntityCommands
{
    [Dependency] private PopupSystem _popupSystem = default!;

    public override string Command => "notify";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _popupSystem.PopupCursor(args[0]);
    }
}
