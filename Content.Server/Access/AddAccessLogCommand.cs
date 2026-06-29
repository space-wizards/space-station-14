using Content.Server.Administration;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Access;

[ToolshedCommand, AdminCommand(AdminFlags.Mapping)]
public sealed class AddAccessLogCommand : ToolshedCommand
{
    [CommandImplementation]
    public void AddAccessLog(IInvocationContext ctx, EntityUid input, float seconds, string accessor)
    {
        var accessReader = EnsureComp<AccessReaderComponent>(input);

        var accessLogCount = accessReader.AccessLog.Count;
        if (accessLogCount >= accessReader.AccessLogLimit)
            ctx.WriteLine($"WARNING: Surpassing the limit of the log by {accessLogCount - accessReader.AccessLogLimit+1} entries!");

        var accessTime = TimeSpan.FromSeconds(seconds);
        EntityManager.System<AccessReaderSystem>().LogAccess((input, accessReader), accessor, accessTime, true);
        ctx.WriteLine($"Successfully added access log to {input} with this information inside:\n " +
                      $"Time of access: {accessTime}\n " +
                      $"Accessed by: {accessor}");
    }

    [CommandImplementation]
    public void AddAccessLogPiped(IInvocationContext ctx, [PipedArgument] EntityUid input, float seconds, string accessor)
    {
        AddAccessLog(ctx, input, seconds, accessor);
    }
}
