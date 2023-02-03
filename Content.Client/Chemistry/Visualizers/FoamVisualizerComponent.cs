using Robust.Client.Animations;
namespace Content.Client.Chemistry.Visualizers;

[RegisterComponent]
[Access(typeof(FoamVisualizerSystem))]
public sealed class FoamVisualizerComponent : Component
{
    public const string AnimationKey = "foamdissolve_animation";

    [DataField("animationTime")]
    public float Delay = 0.6f;

    [DataField("animationState")]
    public string State = "foam-dissolve";

    [ViewVariables(VVAccess.ReadOnly)]
    public Animation Animation = default!;
}
