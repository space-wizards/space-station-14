using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <summary>
/// Spawns a locker, attempts to insert this entity, and welds it shut.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class StuffIntoLockerEntityEffectSystem : EntityEffectSystem<MetaDataComponent, StuffIntoLocker>
{
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<StuffIntoLocker> args)
    {
        var locker = EntityManager.PredictedSpawn(args.Effect.Prototype, _transform.GetMapCoordinates(entity));

        _entityStorage.ToggleOpen(entity.Owner, locker);
        _entityStorage.Insert(entity.Owner, locker);
        _entityStorage.ToggleOpen(entity.Owner, locker);

        _weldable.SetWeldedState(locker, true);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class StuffIntoLocker : EntityEffectBase<StuffIntoLocker>
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
