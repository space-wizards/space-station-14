using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Instantly moves the piped grid next to the target grid, as FTL arrival would.
/// ent 123 | ftlproximity:ftl_launch 456
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class FTLProximityCommand : ToolshedCommand
{
    private ShuttleSystem? _shuttleSystem;

    [CommandImplementation("ftl_launch")]
    public (EntityUid, bool) FtlProximity(
        [PipedArgument] EntityUid gridToTeleportUid,
        EntityUid gridToArriveNearUid)
    {
        _shuttleSystem ??= GetSys<ShuttleSystem>();
        var success = _shuttleSystem.TryFTLProximity(gridToTeleportUid, gridToArriveNearUid);
        return (gridToTeleportUid, success);
    }
}
