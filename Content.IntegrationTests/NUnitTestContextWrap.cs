using System.IO;

namespace Content.IntegrationTests;

/// <summary>
/// Canonical implementation of <see cref="ITestContextLike"/> for usage in actual NUnit tests.
/// </summary>
public sealed class NUnitTestContextWrap(TestContext context, TextWriter writer) : ITestContextLike
{
    public string FullName => context.Test.FullName;
    public TextWriter Out => writer;
}
