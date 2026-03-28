using Content.Shared.RussStation.Surgery.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.RussStation.Surgery.Components;

/// <summary>
/// Tracks an in-progress surgical procedure on a patient.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class ActiveSurgeryComponent : Component
{
    /// <summary>
    /// The procedure being performed.
    /// </summary>
    [AutoNetworkedField, DataField]
    public ProtoId<SurgeryProcedurePrototype>? ProcedureId;

    /// <summary>
    /// The current step index in the procedure.
    /// </summary>
    [AutoNetworkedField, DataField]
    public int CurrentStep;

    /// <summary>
    /// The entity performing the surgery.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Surgeon;
}
