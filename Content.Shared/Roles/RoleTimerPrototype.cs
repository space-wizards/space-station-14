using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// Given to a role to specify its ID for role-timer tracking purposes. That's it.
/// </summary>
[Prototype("roleTimer")]
public sealed class RoleTimerPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;
}
