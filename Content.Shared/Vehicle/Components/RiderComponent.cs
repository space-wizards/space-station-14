using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Added to people when they are riding in a vehicle
/// used mostly to keep track of them for entityquery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class RiderComponent : Component
{
    /// <summary>
    /// The vehicle this rider is currently riding.
    /// </summary>
    [ViewVariables] public EntityUid? Vehicle;

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class RiderComponentState : ComponentState
{
    public EntityUid? Entity;
}
