using Robust.Shared.Prototypes;

namespace Content.Shared.WaterBalloon;

[RegisterComponent]
public sealed partial class WaterBalloonComponent : Component
{
    /// <summary>
    /// Holds the entity thats gonna be spawned after the empty balloon is full
    /// </summary>
    [DataField]
    public EntProtoId FilledPrototype = "FilledWaterBalloon";

}
