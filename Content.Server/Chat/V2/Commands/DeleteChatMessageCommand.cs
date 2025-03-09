using System.Diagnostics;
using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class DeleteChatMessageCommand : ToolshedCommand
{
    [Dependency] private readonly IChatRepository _repository = default!;

    [CommandImplementation("id")]
    public void DeleteChatMessage(IInvocationContext ctx, uint messageId)
    {
        if (!_repository.Delete(messageId))
        {
             ctx.ReportError(new MessageIdDoesNotExist());
        }
    }
}

public record struct MessageIdDoesNotExist() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(Loc.GetString("command-error-deletechatmessage-id-notexist"));
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
