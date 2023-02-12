using System.Threading;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components;

[RegisterComponent]
public sealed class LatticeCuttingComponent : Component
{
    [DataField("toolComponentNeeded")]
    public bool ToolComponentNeeded = true;

    [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Cutting";

    [DataField("delay")]
    public float Delay = 0.25f;

    [DataField("vacuumDelay")]
    public float VacuumDelay = 1.75f;

    /// <summary>
    /// Used for do_afters.
    /// </summary>
    public CancellationTokenSource? CancelTokenSource = null;
}
