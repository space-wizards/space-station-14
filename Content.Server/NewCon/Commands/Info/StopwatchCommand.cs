using Content.Server.NewCon.Syntax;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Commands.Info;

[ConsoleCommand]
public sealed class StopwatchCommand : ConsoleCommand
{
    [CommandImplementation]
    public object? Stopwatch([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] CommandRun expr)
    {
        var watch = new Stopwatch();
        watch.Start();
        var result = expr.Invoke(null, ctx);
        ctx.WriteMarkup($"Ran expression in [color={Color.Aqua.ToHex()}]{watch.Elapsed:g}[/color]");
        return result;
    }
}
