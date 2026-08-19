namespace Content.Shared.Administration.Verbs.Operations.Smites;

public sealed partial class GhostKickOperation : AdminOperationBase<GhostKickOperation>
{
    [DataField(required: true)]
    public LocId Reason { get; private set; }
}
