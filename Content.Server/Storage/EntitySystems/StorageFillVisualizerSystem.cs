using Content.Server.Storage.Components;
using Content.Shared.Rounding;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems;

public sealed class StorageFillVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageFillVisualizerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StorageFillVisualizerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StorageFillVisualizerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInit(EntityUid uid, StorageFillVisualizerComponent component, ComponentInit args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void OnInserted(EntityUid uid, StorageFillVisualizerComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void OnRemoved(EntityUid uid, StorageFillVisualizerComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void UpdateAppearance(EntityUid uid, ServerStorageComponent? storage = null, AppearanceComponent? appearance = null,
        StorageFillVisualizerComponent? component = null)
    {
        if (!Resolve(uid, ref storage, ref appearance, ref component, false))
            return;

        if (component.MaxFillLevels < 1)
            return;

        var level = ContentHelpers.RoundToEqualLevels(storage.StorageUsed, storage.StorageCapacityMax, component.MaxFillLevels);
        _appearance.SetData(uid, StorageFillVisuals.FillLevel, level, appearance);
    }
}
