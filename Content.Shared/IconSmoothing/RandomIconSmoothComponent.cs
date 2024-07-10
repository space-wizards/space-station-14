namespace Content.Shared.IconSmoothing;

/// <summary>
/// Allow randomize StateBase of IconSmoothComponent for random visual variation
/// </summary>
[RegisterComponent]
public sealed partial class RandomIconSmoothComponent : Component
{
    /// <summary>
    /// StateBase will be randomly selected from this list. Allows to randomize the visual.
    /// </summary>
    [DataField(required: true)]
    public List<string> RandomStates = new();
}
