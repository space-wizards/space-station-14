using Robust.Shared.Prototypes;
using Content.Shared.Storage.Components;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnStuffIntoLocker(Entity<MetaDataComponent> entity, ref AdminOperationEvent<StuffIntoLockerOperation> args)
    {
        var locker = Spawn(args.Operation.Prototype, Transform(entity).Coordinates);

        if (TryComp<EntityStorageComponent>(locker, out var storage))
        {
            // Inserting into an open locker drops the target beside it; closing it then captures them normally.
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
            _entityStorage.Insert(entity.Owner, locker, storage);
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
        }

        _weldable.SetWeldedState(locker, true);
    }
}

public sealed partial class StuffIntoLockerOperation : AdminOperationBase<StuffIntoLockerOperation>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }
}
