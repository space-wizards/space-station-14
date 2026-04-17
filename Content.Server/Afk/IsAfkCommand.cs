using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Afk;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class IsAfkCommand : ToolshedCommand
{
    [Dependency] private readonly IAfkManager _afkManager = default!;

    public void IsAfk(IInvocationContext ctx, ICommonSession player)
    {
        ctx.WriteLine(Loc.GetString(_afkManager.IsAfk(player) ? "cmd-isafk-true" : "cmd-isafk-false"));
    }
}
