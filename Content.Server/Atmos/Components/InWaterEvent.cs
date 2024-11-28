using JetBrains.Annotations;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;

namespace Content.Shared.Interaction;

/// <summary>
/// Raised when an entity enters water in the world.
/// </summary>
[PublicAPI]
public sealed class InWaterEvent : HandledEntityEventArgs
{
    /// <summary>
    /// The entity that entered the water.
    /// </summary>
    public EntityUid User { get; }

    public InWaterEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Raised when an entity exits water in the world.
/// </summary>
[PublicAPI]
public sealed class OutOfWaterEvent : HandledEntityEventArgs
{
    /// <summary>
    /// The entity that exited the water.
    /// </summary>
    public EntityUid User { get; }

    public OutOfWaterEvent(EntityUid user)
    {
        User = user;
    }
}
