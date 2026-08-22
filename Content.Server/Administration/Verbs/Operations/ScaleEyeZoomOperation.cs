using Content.Shared.Movement.Components;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnScaleEyeZoom(Entity<MetaDataComponent> entity, ref AdminOperationEvent<ScaleEyeZoomOperation> args)
    {
        var eye = EnsureComp<ContentEyeComponent>(entity);

        _contentEye.SetZoom(
            entity,
            eye.TargetZoom * args.Operation.Factor,
            true,
            eye);
    }
}

public sealed partial class ScaleEyeZoomOperation : AdminOperationBase<ScaleEyeZoomOperation>
{
    [DataField(required: true)]
    public float Factor { get; private set; }
}
