using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

/// <summary>
/// Adds a mind role unless the target already has one from the same prototype.
/// </summary>
public sealed partial class AddMindRoleOperation : AdminOperationBase<AddMindRoleOperation>
{
    [DataField(required: true)]
    public EntProtoId Role { get; private set; }
}
