using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates an entity can be interacted with by an entity with <see cref="StationAiHeldComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationAiSystem))]
public sealed partial class RemoteAccessComponent : Component
{
    /// <summary>
    /// If false, the remote access has been disconnected, but could be restored in the future.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Connected = true;
}
