namespace Content.IntegrationTests;

/// <summary>
/// Attribute that indicates that a string contains yaml prototype data that should be loaded by integration tests.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TestPrototypesAttribute : Attribute
{
}
