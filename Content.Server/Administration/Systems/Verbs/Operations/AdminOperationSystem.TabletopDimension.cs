using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Tabletop.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnTabletopDimension(Entity<MetaDataComponent> entity,
        ref AdminOperationEvent<TabletopDimensionOperation> args)
    {
        var xform = Transform(entity);
        var board = Spawn(args.Operation.Prototype, xform.Coordinates);
        var session = _tabletop.EnsureSession(Comp<TabletopGameComponent>(board));

        _transform.SetMapCoordinates(entity, session.Position);
        _transform.SetWorldRotationNoLerp((entity.Owner, xform), Angle.Zero);
    }
}
