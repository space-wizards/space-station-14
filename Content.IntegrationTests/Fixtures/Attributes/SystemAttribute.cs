#nullable enable
namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Marks a field on a <see cref="GameTest"/> fixture as needing to be populated with a system from the given side.
/// </summary>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

public sealed class SystemAttribute : Attribute
{
    public SystemAttribute(Side side)
    {
        Side = side;

        if (side is not Side.Client and not Side.Server)
        {
            throw new NotSupportedException();
        }
    }

    public Side Side { get; }
}
