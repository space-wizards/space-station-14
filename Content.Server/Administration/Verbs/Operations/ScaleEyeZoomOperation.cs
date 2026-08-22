namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class ScaleEyeZoomOperation : AdminOperationBase<ScaleEyeZoomOperation>
{
    [DataField(required: true)]
    public float Factor { get; private set; }
}
