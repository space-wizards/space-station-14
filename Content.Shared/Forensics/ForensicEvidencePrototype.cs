using Robust.Shared.Prototypes;

namespace Content.Shared.Forensics;
[Prototype("forensicEvidence")]
public sealed partial class ForensicEvidencePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Cleanable { get; private set; } = true;

    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public LocId Title { get; private set; } = string.Empty;
}
