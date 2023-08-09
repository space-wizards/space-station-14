using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components;

[RegisterComponent]
public sealed class LatticeCuttingComponent : Component
{
    [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Cutting";

    [DataField("delay")]
    public float Delay = 1f;

    [DataField("vacuumDelay")]
    public float VacuumDelay = 1.75f;
}
