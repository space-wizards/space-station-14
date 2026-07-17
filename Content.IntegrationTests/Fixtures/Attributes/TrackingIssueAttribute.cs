using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

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
/// <para>
///     If the bug was never given an issue, the fix PR containing the test is another acceptable thing to link, and the
///     PR should clearly explain the bug it is fixing for future readers.
/// </para>
/// </summary>
public sealed class TrackingIssueAttribute : PropertyAttribute
{
    /// <summary>
    ///     Domains we allow for tracking issues, to avoid people putting discord or discourse links.
    /// </summary>
    private static readonly string[] _validDomains =
    [
        "github.com"
    ];

    private static readonly Regex GithubStyleIssueMatch = new(@"^\/[a-z\d\-\$\#]*\/[a-z\d\-\$\#]*\/(issues|pulls)\/\d*$",
        RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.IgnoreCase);

    public TrackingIssueAttribute([StringSyntax(StringSyntaxAttribute.Uri)] string url) : base(url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Expected a valid URL for {nameof(TrackingIssueAttribute)}, got {url}");

        // Assert the domain is reasonable.
        if (!_validDomains.Contains(uri.Host, StringComparer.InvariantCultureIgnoreCase))
        {
            throw new ArgumentException(
                $"Didn't recognize the domain used for the tracking issue, got {uri.Host}. We support: {string.Join(", ", _validDomains)}");
        }

        // Assert that the URL is reasonable.
        if (!GithubStyleIssueMatch.IsMatch(uri.AbsolutePath))
        {
            throw new ArgumentException(
                $"Didn't recognize the provided github link, it should point to a specific pull request or issue. Got {uri.AbsolutePath}");
        }
    }
}
