#nullable enable

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Marks a field on a <see cref="GameTest"/> fixture as needing to be populated with an IoC dependency from the given side.
/// </summary>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SidedDependencyAttribute : Attribute
{
    public SidedDependencyAttribute(Side side)
    {
        Side = side;

        if (side is not Side.Client and not Side.Server)
        {
            throw new NotSupportedException();
        }
    }

    public Side Side { get; }
}
