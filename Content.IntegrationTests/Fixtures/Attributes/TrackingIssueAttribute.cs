using System.Diagnostics.CodeAnalysis;

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
/// <para>
///     An attribute meant to attach an issue (usually related to the test) to a given test or test fixture.
///     This sets the <c>TrackingIssue</c> property on the test, and helps developers find why a test exists or why it
///     is broken.
/// </para>
/// <para>
///     This attribute should be used if a test corresponds directly to a bug in some way, either demonstrating it or
///     ensuring it remains fixed. Only URLs should be provided, lone issue numbers are not accepted.
/// </para>
/// </summary>
public sealed class TrackingIssueAttribute : PropertyAttribute
{
    public TrackingIssueAttribute([StringSyntax(StringSyntaxAttribute.Uri)] string url) : base(url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException($"Expected a valid URL for {nameof(TrackingIssueAttribute)}, got {url}");
    }
}
