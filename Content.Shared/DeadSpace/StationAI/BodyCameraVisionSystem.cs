// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.UniformAccessories.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StationAi;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared.DeadSpace.StationAi;

public sealed class BodyCameraVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string BodyCameraTag = "BodyCamera";
    private const SlotFlags AccessoryWearSlots = SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiVisionComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<StationAiVisionComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<GotEquippedEvent>(OnAnyEquipped);
        SubscribeLocalEvent<GotUnequippedEvent>(OnAnyUnequipped);
        SubscribeLocalEvent<EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    private void OnEquipped(EntityUid uid, StationAiVisionComponent component, GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.NECK) == 0 || !_tag.HasTag(uid, BodyCameraTag))
            return;

        AddSource(args.Equipee, (uid, component));
    }

    private void OnUnequipped(EntityUid uid, StationAiVisionComponent component, GotUnequippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.NECK) == 0 || !_tag.HasTag(uid, BodyCameraTag))
            return;

        RemoveSource(args.Equipee, uid);
    }

    private void OnAnyEquipped(GotEquippedEvent args)
    {
        if ((args.SlotFlags & AccessoryWearSlots) == 0 ||
            !TryComp<UniformAccessoryHolderComponent>(args.Equipment, out var holder))
            return;

        AddHolderSources(args.Equipee, holder);
    }

    private void OnAnyUnequipped(GotUnequippedEvent args)
    {
        if ((args.SlotFlags & AccessoryWearSlots) == 0 ||
            !TryComp<UniformAccessoryHolderComponent>(args.Equipment, out var holder))
            return;

        RemoveHolderSources(args.Equipee, holder);
    }

    private void OnContainerInserted(EntInsertedIntoContainerMessage args)
    {
        if (!IsAccessoryContainer(args.Container) ||
            !TryGetAccessoryWearer(args.Container.Owner, out var wearer) ||
            !TryComp<StationAiVisionComponent>(args.Entity, out var vision) ||
            !_tag.HasTag(args.Entity, BodyCameraTag))
            return;

        AddSource(wearer, (args.Entity, vision));
    }

    private void OnContainerRemoved(EntRemovedFromContainerMessage args)
    {
        if (!IsAccessoryContainer(args.Container) ||
            !TryGetAccessoryWearer(args.Container.Owner, out var wearer) ||
            !_tag.HasTag(args.Entity, BodyCameraTag))
            return;

        RemoveSource(wearer, args.Entity);
    }

    private bool IsAccessoryContainer(BaseContainer container)
    {
        return container.ID == UniformAccessoryHolderComponent.ContainerId &&
               HasComp<UniformAccessoryHolderComponent>(container.Owner);
    }

    private bool TryGetAccessoryWearer(EntityUid holder, out EntityUid wearer)
    {
        wearer = default;
        if (!_container.TryGetContainingContainer((holder, null, null), out var equipmentContainer) ||
            !_inventory.TryGetSlot(equipmentContainer.Owner, equipmentContainer.ID, out var slot) ||
            (slot.SlotFlags & AccessoryWearSlots) == 0)
            return false;

        wearer = equipmentContainer.Owner;
        return true;
    }

    private void AddHolderSources(EntityUid wearer, UniformAccessoryHolderComponent holder)
    {
        if (holder.AccessoryContainer == null)
            return;

        foreach (var accessory in holder.AccessoryContainer.ContainedEntities)
        {
            if (!TryComp<StationAiVisionComponent>(accessory, out var vision) ||
                !_tag.HasTag(accessory, BodyCameraTag))
                continue;

            AddSource(wearer, (accessory, vision));
        }
    }

    private void RemoveHolderSources(EntityUid wearer, UniformAccessoryHolderComponent holder)
    {
        if (holder.AccessoryContainer == null)
            return;

        foreach (var accessory in holder.AccessoryContainer.ContainedEntities)
        {
            if (_tag.HasTag(accessory, BodyCameraTag))
                RemoveSource(wearer, accessory);
        }
    }

    private void AddSource(EntityUid wearer, Entity<StationAiVisionComponent> source)
    {
        var tracker = EnsureComp<BodyCameraVisionComponent>(wearer);
        if (!tracker.Sources.Add(source.Owner))
            return;

        if (tracker.Sources.Count == 1)
        {
            if (TryComp<StationAiVisionComponent>(wearer, out var originalVision))
            {
                tracker.OriginalEnabled = originalVision.Enabled;
                tracker.OriginalOccluded = originalVision.Occluded;
                tracker.OriginalRange = originalVision.Range;
            }
            else
            {
                tracker.AddedVisionComponent = true;
                AddComp<StationAiVisionComponent>(wearer);
            }
        }

        var vision = Comp<StationAiVisionComponent>(wearer);
        RefreshVision((wearer, vision), tracker);
    }

    private void RemoveSource(EntityUid wearer, EntityUid source)
    {
        if (!TryComp<BodyCameraVisionComponent>(wearer, out var tracker) ||
            !tracker.Sources.Remove(source))
            return;

        if (tracker.Sources.Count > 0)
        {
            if (TryComp<StationAiVisionComponent>(wearer, out var vision))
                RefreshVision((wearer, vision), tracker);
            return;
        }

        if (!tracker.AddedVisionComponent &&
            TryComp<StationAiVisionComponent>(wearer, out var originalVision))
        {
            _stationAi.SetVisionRange((wearer, originalVision), tracker.OriginalRange);
            _stationAi.SetVisionOccluded((wearer, originalVision), tracker.OriginalOccluded);
            _stationAi.SetVisionEnabled((wearer, originalVision), tracker.OriginalEnabled);
        }

        RemComp<BodyCameraVisionComponent>(wearer);

        if (tracker.AddedVisionComponent)
            RemComp<StationAiVisionComponent>(wearer);
    }

    private void RefreshVision(
        Entity<StationAiVisionComponent> wearer,
        BodyCameraVisionComponent tracker)
    {
        var enabled = !tracker.AddedVisionComponent && tracker.OriginalEnabled;
        var range = enabled ? tracker.OriginalRange : 0f;
        var occluded = !enabled || tracker.OriginalOccluded;

        foreach (var source in tracker.Sources)
        {
            if (!TryComp<StationAiVisionComponent>(source, out var sourceVision) ||
                !sourceVision.Enabled)
                continue;

            if (!enabled)
            {
                enabled = true;
                range = sourceVision.Range;
                occluded = sourceVision.Occluded;
                continue;
            }

            range = Math.Max(range, sourceVision.Range);
            occluded &= sourceVision.Occluded;
        }

        _stationAi.SetVisionRange(wearer, range);
        _stationAi.SetVisionOccluded(wearer, occluded);
        _stationAi.SetVisionEnabled(wearer, enabled);
    }
}
