using JetBrains.Annotations;

namespace Content.Shared.Administration;

/// <summary>
///     Specifies that a command can only be executed by an admin with the specified flags.
/// </summary>
/// <remarks>
///     If this attribute is used multiple times, either attribute's flag sets can be used to get access.
/// </remarks>
/// <seealso cref="AnyCommandAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
[MeansImplicitUse]
public sealed class AdminCommandAttribute(AdminFlags flags) : Attribute
{
    public AdminFlags Flags { get; } = flags;
}
