using Robust.Shared.GameStates;

namespace Content.Shared.StationAi;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Occluded = false;

    /// <summary>
    /// Range in tiles
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 7.5f;
}
