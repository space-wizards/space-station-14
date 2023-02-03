using Robust.Client.Animations;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// 
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
    /// 
    /// </summary>
    [DataField("animation_time")]
    public float Delay = 0.25f;

    /// <summary>
    /// 
    /// </summary>
    [DataField("animation_state")]
    public string State = "chempuff";

    /// <summary>
    /// 
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Animation VaporFlick = default!;
}
