namespace Content.Shared.Administration.Verbs.Operations;

public sealed partial class SetGodmodeOperation : AdminOperationBase<SetGodmodeOperation>
{
    [DataField(required: true)]
    public bool Enabled { get; private set; }
}
