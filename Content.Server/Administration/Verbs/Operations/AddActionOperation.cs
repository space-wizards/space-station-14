using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

/// <summary>
/// Adds an action unless the target already has one from the same prototype.
/// </summary>
public sealed partial class AddActionOperation : AdminOperationBase<AddActionOperation>
{
    [DataField(required: true)]
    public EntProtoId Action { get; private set; }
}
