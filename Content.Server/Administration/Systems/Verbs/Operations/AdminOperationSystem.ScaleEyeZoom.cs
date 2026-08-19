using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Movement.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

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
