using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Client.Chemistry.Visualizers.Foam;

[UsedImplicitly]
[RegisterComponent]
public sealed class FoamVisualsComponent : Component, ISerializationHooks
{
    [DataField ("animationTime")]
    public float AnimationTime = 0.6f;

    [DataField ("animationState")]
    public string AnimationState = "foam-dissolve";
}

public enum FoamVisualLayers : byte
{
    Base
}
