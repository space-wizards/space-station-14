using Robust.Shared.Serialization;

namespace Content.Shared._FTL.PowerControl;

/// <summary>
/// This is used for...
/// </summary>
public abstract class SharedPowerControlComponent : Component
{

}

/// <summary>
/// Contains network state for SharedPowerControlComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class SharedPowerControlComponentState : ComponentState
{
    public SharedPowerControlComponentState(SharedPowerControlComponent component)
    {

    }
}
