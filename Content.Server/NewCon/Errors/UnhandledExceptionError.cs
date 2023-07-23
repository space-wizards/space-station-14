using Robust.Shared.Utility;

namespace Content.Server.NewCon.Errors;

public sealed class UnhandledExceptionError : IConError
{
    public Exception Exception;
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText(Exception.ToString());
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }

    public UnhandledExceptionError(Exception exception)
    {
        Exception = exception;
    }
}
