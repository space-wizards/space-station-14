using System.IO;

namespace Content.IntegrationTests;

/// <summary>
/// Something that looks like a <see cref="TestContext"/>, for passing to integration tests.
/// </summary>
public interface ITestContextLike
{
    string FullName { get; }
    TextWriter Out { get; }
}

