namespace Content.Server.NewCon.Commands.Players;

[ConsoleCommand]
public sealed class SelfCommand : ConsoleCommand
{
    [CommandImplementation]
    public EntityUid Self([CommandInvocationContext] IInvocationContext ctx)
    {
        if (ctx.Session?.AttachedEntity is not { } ent)
            throw new Exception("No player entity to be self.");

        return ent;
    }
}
