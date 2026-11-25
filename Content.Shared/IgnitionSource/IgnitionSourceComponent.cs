using Robust.Shared.GameStates;

namespace Content.Shared.IgnitionSource;

/// <summary>
/// This is used for creating atmosphere hotspots while ignited to start reactions such as fire.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedIgnitionSourceSystem))]
public sealed partial class IgnitionSourceComponent : Component
{
    /// <summary>
    /// Is this source currently ignited?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Ignited;

    /// <summary>
    /// The temperature used when creating atmos hotspots.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature = 700f;
}
