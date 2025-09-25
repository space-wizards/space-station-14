using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Component for entities that can diagnose diseases.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseDiagnoserComponent : Component
{
    [DataField]
    public EntProtoId PaperPrototype = "DiagnosisReportPaper";
}
