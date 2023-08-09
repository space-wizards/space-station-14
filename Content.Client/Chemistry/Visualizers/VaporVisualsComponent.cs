using Robust.Client.Animations;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// A component that plays an animation when it is sprayed.
/// </summary>
[RegisterComponent]
[Access(typeof(VaporVisualizerSystem))]
public sealed class VaporVisualsComponent : Component
{
    /// <summary>
    /// The id of the animation played when the vapor spawns in.
    /// </summary>
    public const string AnimationKey = "flick_animation";

    /// <summary>
    /// The amount of time over which the spray animation is played.
    /// </summary>
    [DataField("animationTime")]
    public float AnimationTime = 0.25f;

    /// <summary>
    /// The RSI state that is flicked when the vapor is sprayed.
    /// </summary>
    [DataField("animationState")]
    public string AnimationState = "chempuff";

    /// <summary>
    /// The animation that plays when the vapor is sprayed.
    /// Generated in <see cref="VaporVisualizerSystem.OnComponentInit"/>
    /// </summary>
    public Animation VaporFlick = default!;
}
