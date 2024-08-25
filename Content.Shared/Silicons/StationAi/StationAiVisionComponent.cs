using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]//, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Occluded = true;

    /// <summary>
    /// Range in tiles
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 7.5f;
}
