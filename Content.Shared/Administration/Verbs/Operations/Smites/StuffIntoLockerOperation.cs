using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

public sealed partial class StuffIntoLockerOperation : AdminOperationBase<StuffIntoLockerOperation>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }
}
