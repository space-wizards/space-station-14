using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.WaterBalloon;

[RegisterComponent]
public sealed partial class WaterBalloonComponent : Component
{
    [DataField]
    public FixedPoint2 MaxVolume = 10;

    [DataField]
    public bool Filled = false;

    [DataField]
    public string FilledPrototype = "FilledWaterBalloon";

}
