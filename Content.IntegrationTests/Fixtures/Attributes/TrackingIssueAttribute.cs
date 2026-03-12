using System.Diagnostics.CodeAnalysis;

namespace Content.IntegrationTests.Fixtures.Attributes;

public sealed class TrackingIssueAttribute : PropertyAttribute
{
    public TrackingIssueAttribute([StringSyntax(StringSyntaxAttribute.Uri)] string url) : base(url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException($"Expected a valid URL for {nameof(TrackingIssueAttribute)}, got {url}");
    }
}
