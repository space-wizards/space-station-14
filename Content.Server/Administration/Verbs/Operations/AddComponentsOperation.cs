using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AddComponentsOperation : AdminOperationBase<AddComponentsOperation>
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField]
    public bool ReplaceExisting { get; private set; }
}
