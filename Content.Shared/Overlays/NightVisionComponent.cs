using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public string Color;

    [DataField("luminancethreshold")]
    public float LuminanceThreshold;

    [DataField("noiseamount")]
    public float NoiseAmount;

    [DataField("tint")]
    public Vector3 Tint = new(0.3f, 0.3f, 0.3f);


}
