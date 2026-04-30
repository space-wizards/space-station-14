using Content.Shared.Light.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
///     Marks component that receive illumination from <see cref="SharedPointLightComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedLightSensitiveSystem)), AutoGenerateComponentState(true)]
public sealed partial class LightSensitiveComponent : Component
{

    /// <summary>
    ///     Current illumination value as a percentage.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float LightLevel = 0f;

    /// <summary>
    ///     When this Entity last had its light level evaluated. 
    ///To prevent multiple expensive updates per tick.
    ////// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastUpdate = TimeSpan.Zero;

}
