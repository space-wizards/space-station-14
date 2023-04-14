using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Treatments.Prototypes;

[Prototype("treatment")]
public sealed class TreatmentTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; set; } = "";

    [DataField("repeatable", required: true)]
    public bool Repeatable;
}
