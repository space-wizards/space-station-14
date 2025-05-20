using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.JobBoard;

/// <summary>
/// Marks a label for a bounty for a given salvage job board prototype.
/// </summary>
[RegisterComponent]
public sealed partial class JobBoardLabelComponent : Component
{
    /// <summary>
    /// The bounty corresponding to this label.
    /// </summary>
    [DataField]
    public ProtoId<CargoBountyPrototype>? JobId;
}
