using Robust.Shared.Containers;

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
    public ContainerSlot IdentityEntitySlot = default!;
}
