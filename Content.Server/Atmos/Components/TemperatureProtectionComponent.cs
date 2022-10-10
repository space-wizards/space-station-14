using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed class TemperatureProtectionComponent : Component, IExamineGroup
{
    /// <summary>
    ///     How much to multiply temperature deltas by.
    /// </summary>
    [DataField("coefficient")]
    public float Coefficient = 1.0f;

    [DataField("examineGroup", customTypeSerializer: typeof(PrototypeIdSerializer<ExamineGroupPrototype>))]
    public string ExamineGroup { get; set; } = "atmos";

    [DataField("examinePriority")]
    public float ExaminePriority { get; set; } = 1.0f;
}
