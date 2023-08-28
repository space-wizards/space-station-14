using Content.Server.Mind.Components;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Players;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.Mind.Toolshed;

/// <summary>
///     Contains various mind-manipulation commands like getting minds, controlling mobs, etc.
/// </summary>
[ToolshedCommand]
public sealed class MindCommand : ToolshedCommand
{
    private MindSystem? _mind;

    [CommandImplementation("get")]
    public Mind? Get([PipedArgument] IPlayerSession session)
    {
        return session.ContentData()?.Mind;
    }

    [CommandImplementation("get")]
    public Mind? Get([PipedArgument] EntityUid ent)
    {
        if (!TryComp<MindContainerComponent>(ent, out var container))
        {
            return null;
        }

        return container.Mind;
    }

    [CommandImplementation("control")]
    public EntityUid Control(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] EntityUid target,
            [CommandArgument] ValueRef<IPlayerSession> playerRef)
    {
        _mind ??= GetSys<MindSystem>();

        var player = playerRef.Evaluate(ctx);
        if (player is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return target;
        }

        var mind = player.ContentData()?.Mind;

        if (mind is null)
        {
            ctx.ReportError(new SessionHasNoEntityError(player));
            return target;
        }

        _mind.TransferTo(mind, target);
        return target;
    }
}
