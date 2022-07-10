using Robust.Shared.Containers;
using Robust.Shared.Enums;

namespace Content.Shared.Identity.Components;

/// <summary>
///     Stores the identity entity (whose name is the users 'identity', etc)
///     for a given entity, and marks that it can have an identity at all.
/// </summary>
/// <remarks>
///     This is a <see cref="ContainerSlot"/> and not just a datum entity because we do sort of care that it gets deleted with the user.
/// </remarks>
public sealed class IdentityComponent : Component
{
    [ViewVariables]
    public ContainerSlot IdentityEntitySlot = default!;
}
