using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// Interact with to use ship guns.
/// </summary>
[NetworkedComponent]
public abstract class SharedTurretConsoleComponent : Component
{

}

[Serializable, NetSerializable]
public enum TurretConsoleUiKey : byte
{
    Key,
}
