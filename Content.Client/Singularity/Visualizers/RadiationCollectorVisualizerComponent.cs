using Robust.Client.Animations;

namespace Content.Client.Singularity.Visualizers;

[RegisterComponent]
[Access(typeof(RadiationCollectorVisualizerSystem))]
public sealed class RadiationCollectorVisualizerComponent : Component
{
    public const string AnimationKey = "radiationcollector_animation";

    public Animation ActivateAnimation = default!;
    public Animation DeactiveAnimation = default!;
}
