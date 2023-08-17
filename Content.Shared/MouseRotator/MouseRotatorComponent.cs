using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MouseRotator;

/// <summary>
/// This component allows overriding an entities local rotation based on the client's mouse movement
/// </summary>
/// <see cref="SharedMouseRotatorSystem"/>
[RegisterComponent, NetworkedComponent]
public sealed class MouseRotatorComponent : Component
{
}

/// <summary>
///     Raised on an entity with <see cref="MouseRotatorComponent"/> as a predictive event on the client
///     when mouse rotation changes
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestMouseRotatorRotationEvent : EntityEventArgs
{
    public Angle Rotation;
}
