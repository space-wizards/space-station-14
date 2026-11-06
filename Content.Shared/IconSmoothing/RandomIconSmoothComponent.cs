using Robust.Shared.GameStates;

namespace Content.Shared.IconSmoothing;

/// <summary>
/// Allow randomize StateBase of IconSmoothComponent for random visual variation
/// TODO: Make this an interface on ISpriteSmoothState? Base Key?
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomIconSmoothComponent : Component
{
    /// <summary>
    /// Declares the index of the <see cref="ISpriteSmoothState"/> this component searches for and smooths with.
    /// </summary>
    [DataField]
    public int Index;

    /// <summary>
    /// StateBase will be randomly selected from this list. Allows to randomize the visual.
    /// </summary>
    [DataField(required: true)]
    public List<string> RandomStates = new();
}
