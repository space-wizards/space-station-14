using Robust.Shared.Utility;

namespace Content.Server.NewCon.Errors;

public sealed class NotForServerConsoleError : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            "You must be logged in with a client to use this, the server console isn't workable.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
