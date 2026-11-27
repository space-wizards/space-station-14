using Content.Shared.Resist.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Resist.Components;

/// <summary>
/// Allows an entity to escape from a containing inventory or storage container by moving.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(EscapeInventorySystem))]
public sealed partial class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// Whether the entity is currently trying to escape.
    /// </summary>
    public bool IsEscaping => DoAfterIndex != null;

    /// <summary>
    /// Base duration, in seconds, of an escape attempt.
    /// </summary>
    [DataField]
    public float BaseResistTime = 5f;

    /// <summary>
    /// Index of the active escape DoAfter.
    /// TODO: Replace with DoAfterId when it supports networked
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort? DoAfterIndex;
}
