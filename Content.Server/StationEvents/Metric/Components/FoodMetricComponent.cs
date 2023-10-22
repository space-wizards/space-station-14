using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(FoodMetricSystem))]
public sealed partial class FoodMetricComponent : Component
{
    [DataField("thirstScores", customTypeSerializer: typeof(DictionarySerializer<ThirstThreshold, FixedPoint2>)),
     ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ThirstThreshold, FixedPoint2> ThirstScores =
        new()
        {
            { ThirstThreshold.Thirsty, 2.0f },
            { ThirstThreshold.Parched, 5.0f },
        };

    [DataField("hungerScores", customTypeSerializer: typeof(DictionarySerializer<HungerThreshold, FixedPoint2>)),
     ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<HungerThreshold, FixedPoint2> HungerScores =
        new()
        {
            { HungerThreshold.Peckish, 2.0f },
            { HungerThreshold.Starving, 5.0f },
        };
}
