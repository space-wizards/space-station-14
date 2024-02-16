using Content.Server.Administration;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.Access;

[ToolshedCommand, AdminCommand(AdminFlags.Mapping)]
public sealed class AddAccessLogCommand : ToolshedCommand
{
    [CommandImplementation]
    public void AddAccessLog(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] EntityUid input,
        [CommandArgument] float seconds,
        [CommandArgument] ValueRef<string> accessor)
    {
        var accessReader = EnsureComp<AccessReaderComponent>(input);

        var accessLogCount = accessReader.AccessLog.Count;
        if (accessLogCount >= accessReader.AccessLogLimit)
            ctx.WriteLine($"WARNING: Surpassing the limit of the log by {accessLogCount - accessReader.AccessLogLimit+1} entries!");

        var accessTime = TimeSpan.FromSeconds(seconds);
        var accessName = accessor.Evaluate(ctx)!;
        accessReader.AccessLog.Enqueue(new AccessRecord(accessTime, accessName));
        ctx.WriteLine($"Successfully added access log to {input} with this information inside:\n " +
                      $"Time of access: {accessTime}\n " +
                      $"Accessed by: {accessName}");
    }

    [CommandImplementation]
    public void AddAccessLogPiped(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] float seconds,
        [CommandArgument] ValueRef<string> accessor)
    {
        AddAccessLog(ctx, input, seconds, accessor);
    }
}
