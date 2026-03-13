#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Configures the test pair using settings from the given type (by default the current test) and static property member.
/// </summary>
/// <param name="sourceType">The type to look up the member on, if any.</param>
/// <param name="sourceMember">The static property to read the settings from.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PairConfigAttribute(Type? sourceType, string sourceMember) : Attribute, IGameTestPairConfigModifier
{
    public bool Exclusive => true;
    public IReadOnlySet<string> AffectedProperties { get; } = new HashSet<string>();

    public readonly Type? SourceType = sourceType;
    public readonly string SourceMember = sourceMember;

    private const BindingFlags PropertyBindingFlags = BindingFlags.Static
                                                      | BindingFlags.Public
                                                      | BindingFlags.NonPublic
                                                      | BindingFlags.FlattenHierarchy;

    public PairConfigAttribute(string sourceMember) : this(null, sourceMember)
    {
    }

    public void ApplyToPairSettings(GameTest test, ref PoolSettings settings)
    {
        var sourceType = SourceType ?? test.GetType();

        var field = sourceType.GetProperty(SourceMember, PropertyBindingFlags);

        if (field is null)
        {
            if (sourceType.GetField(SourceMember, PropertyBindingFlags) is not null)
            {
                throw new ArgumentException(
                    $"Couldn't find static property {SourceMember} on {sourceType.Name}, but could find a field. Only properties are allowed.");
            }

            throw new ArgumentException($"Couldn't find static property {SourceMember} on {sourceType.Name}");
        }

        if (!field.PropertyType.IsAssignableTo(typeof(PoolSettings)))
            throw new ArgumentException(
                $"{sourceType.Name}.{SourceMember} is not assignable to {nameof(PoolSettings)} and cannot be used.");

        settings = (PoolSettings)field.GetValue(null)!;
    }
}
