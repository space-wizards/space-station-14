using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(PuddleMetricSystem))]
public sealed partial class PuddleMetricComponent : Component
{
    /// <summary>
    ///   The cost of each puddle, per mL. Note about 200 mL is one puddle.
    /// </summary>
    [DataField("puddles", customTypeSerializer: typeof(DictionarySerializer<string, FixedPoint2>)), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, FixedPoint2> Puddles =
        new()
        {
            { "Water", 0.02 },
            { "SpaceCleaner", 0.02f },

            { "Nutriment", 0.1f },
            { "Sugar", 0.1f },
            { "Ephedrine", 0.1f },
            { "Ale", 0.1f },
            { "Beer", 0.1f },

            { "Slime", 0.2f },
            { "Blood", 0.2f },
            { "SpaceDrugs", 0.3f },
            { "SpaceLube", 0.3f },
        };

    [DataField("puddleDefault"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PuddleDefault = 0.1f;

}
