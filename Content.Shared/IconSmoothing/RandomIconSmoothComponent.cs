using Robust.Shared.GameStates;

namespace Content.Shared.IconSmoothing;

/// <summary>
/// Allow randomize StateBase of IconSmoothComponent for random visual variation
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RandomIconSmoothComponent : Component
{
    /// <summary>
    /// StateBase will be randomly selected from this list. Allows to randomize the visual.
    /// </summary>
    [DataField(required: true)]
    public List<string> RandomStates = new();

    /// <summary>
    /// save information about the selected state on the server for synchronization between clients
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SelectedState = string.Empty;
}
