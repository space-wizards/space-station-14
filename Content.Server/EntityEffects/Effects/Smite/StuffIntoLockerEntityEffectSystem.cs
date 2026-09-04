using Content.Server.Storage.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class StuffIntoLockerEntityEffectSystem : EntityEffectSystem<MetaDataComponent, StuffIntoLocker>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<StuffIntoLocker> args)
    {
        var locker = Spawn(args.Effect.Prototype, Transform(entity).Coordinates);

        if (TryComp<EntityStorageComponent>(locker, out var storage))
        {
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
            _entityStorage.Insert(entity.Owner, locker, storage);
            _entityStorage.ToggleOpen(entity.Owner, locker, storage);
        }

        _weldable.SetWeldedState(locker, true);
    }
}

public sealed partial class StuffIntoLocker : EntityEffectBase<StuffIntoLocker>
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
