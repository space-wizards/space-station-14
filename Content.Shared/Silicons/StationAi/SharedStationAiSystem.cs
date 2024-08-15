using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract class SharedStationAiSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    /*
     * TODO: Sprite / vismask visibility
     */

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiComponent, EntInsertedIntoContainerMessage>(OnAiInsert);
        SubscribeLocalEvent<StationAiComponent, EntRemovedFromContainerMessage>(OnAiRemove);
        SubscribeLocalEvent<StationAiComponent, MapInitEvent>(OnAiMapInit);
        SubscribeLocalEvent<StationAiComponent, ComponentShutdown>(OnAiShutdown);
    }

    private void OnAiShutdown(Entity<StationAiComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.RemoteEntity);
        ent.Comp.RemoteEntity = null;
    }

    private void OnAiMapInit(Entity<StationAiComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance((ent.Owner, ent.Comp));
        SetupEye(ent);
        AttachEye(ent);
    }

    private void SetupEye(Entity<StationAiComponent> ent)
    {
        if (ent.Comp.RemoteEntityProto != null)
        {
            ent.Comp.RemoteEntity = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(ent.Owner).Coordinates);
        }
    }

    private void AttachEye(Entity<StationAiComponent> ent)
    {
        if (ent.Comp.RemoteEntity == null)
            return;

        if (!_containers.TryGetContainer(ent.Owner, StationAiComponent.Container, out var container) ||
            container.ContainedEntities.Count != 1)
        {
            return;
        }

        if (TryComp(container.ContainedEntities[0], out EyeComponent? eyeComp))
        {
            _eye.SetTarget(container.ContainedEntities[0], ent.Comp.RemoteEntity.Value, eyeComp);
        }

        _mover.SetRelay(container.ContainedEntities[0], ent.Comp.RemoteEntity.Value);
    }

    private void OnAiInsert(Entity<StationAiComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Just so text and the likes works properly
        _metadata.SetEntityName(ent.Owner, MetaData(args.Entity).EntityName);

        EnsureComp<StationAiOverlayComponent>(args.Entity);
        UpdateAppearance((ent.Owner, ent.Comp));

        AttachEye(ent);
    }

    private void OnAiRemove(Entity<StationAiComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Reset name to whatever
        _metadata.SetEntityName(ent.Owner, Prototype(ent.Owner)?.Name ?? string.Empty);

        // Remove eye relay
        RemCompDeferred<RelayInputMoverComponent>(args.Entity);

        if (TryComp(args.Entity, out EyeComponent? eyeComp))
        {
            _eye.SetTarget(args.Entity, null, eyeComp);
        }

        RemCompDeferred<StationAiOverlayComponent>(args.Entity);
        UpdateAppearance((ent.Owner, ent.Comp));
    }

    public void UpdateAppearance(Entity<StationAiComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (!_containers.TryGetContainer(entity.Owner, StationAiComponent.Container, out var container) ||
            container.Count == 0)
        {
            Appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Empty);
            return;
        }

        Appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Occupied);
    }
}

[Serializable, NetSerializable]
public enum StationAiVisualState : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum StationAiState : byte
{
    Empty,
    Occupied,
    Dead,
}
