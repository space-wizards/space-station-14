using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ShowGhostsCommand : ToolshedCommand
{
    [Dependency] private readonly SharedGhostVisibilitySystem _ghostVis = default!;

    [CommandImplementation]
    public void ShowGhosts(bool visible) => _ghostVis.SetAllVisible(visible);
}
