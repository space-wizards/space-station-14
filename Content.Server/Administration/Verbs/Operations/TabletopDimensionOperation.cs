using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

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

public sealed partial class TabletopDimensionOperation : AdminOperationBase<TabletopDimensionOperation>
{
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype { get; private set; }
}
