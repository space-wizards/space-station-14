using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// This is used for...
/// </summary>
public abstract class SharedTurretConsoleComponent : Component
{

}

/// <summary>
/// Contains network state for SharedTurretControlComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class SharedTurretConsoleComponentState : ComponentState
{
    public SharedTurretConsoleComponentState(SharedTurretConsoleComponent component)
    {

    }
}
