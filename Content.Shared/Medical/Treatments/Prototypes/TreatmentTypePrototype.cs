using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Treatments.Prototypes;

[Prototype("treatmentType")]
public sealed class TreatmentTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; set; } = "";

    [DataField("repeatable", required: true)]
    public bool Repeatable;
}
