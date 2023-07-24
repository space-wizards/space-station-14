using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Errors;

public record struct SessionHasNoEntityError(ICommonSession Session) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"The user {Session.Name} has no attached entity.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
