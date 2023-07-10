using Content.Shared.Spreader;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Anomaly.Effects.Components;

/// <summary>
/// This is used for an anomaly that controls the spread of a specific <see cref="EdgeSpreaderPrototype"/>
/// </summary>
[RegisterComponent, Access(typeof(SpreaderAnomalySystem))]
public sealed class SpreaderAnomalyComponent : Component
{
    [DataField("group", customTypeSerializer: typeof(PrototypeIdSerializer<EdgeSpreaderPrototype>))]
    public string Group = "anomalousSpreader";

    [DataField("minSpreadRadius")]
    public float MinSpreadRadius = 2;

    [DataField("maxSpreadRadius")]
    public float MaxSpreadRadius = 5;

    public float MinUpdatesPerSecond = 1;

    public float MaxUpdatesPerSecond = 3;
}
