using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Silicons.StationAi;

public abstract class SharedStationAiSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    /*
     * TODO: Fix examines
     * TODO: Vismask on the AI eye
     * TODO: Add door bolting
     * TODO: Sprite / vismask visibility + action
     * TODO: Double-check positronic interactions didn't break
     * Need action bar
     * AI wire for doors.
     * Need to check giving comp "just works" and maybe map act for it
     * Need to move the view stuff to shared + parallel + optimise
     * Need cameras to be snip-snippable and to turn ai vision off, also xray support, also power
     * Need to bump PVS range for AI to like 20 or something
     * Make it a screen-space overlay
     * Need interaction whitelist or something working
     * Need destruction and all that
     */

    // StationAiHeld is added to anything inside of an AI core.
    // StationAiHolder indicates it can hold an AI positronic brain (e.g. holocard / core).
    // StationAiCore holds functionality related to the core itself.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiHeldComponent, AccessibleOverrideEvent>(OnAiAccessible);
        SubscribeLocalEvent<StationAiHeldComponent, InteractionAttemptEvent>(OnAiInteraction);

        SubscribeLocalEvent<StationAiHolderComponent, ComponentInit>(OnHolderInit);
        SubscribeLocalEvent<StationAiHolderComponent, ComponentRemove>(OnHolderRemove);
        SubscribeLocalEvent<StationAiHolderComponent, AfterInteractEvent>(OnHolderInteract);
        SubscribeLocalEvent<StationAiHolderComponent, MapInitEvent>(OnHolderMapInit);
        SubscribeLocalEvent<StationAiHolderComponent, EntInsertedIntoContainerMessage>(OnHolderConInsert);
        SubscribeLocalEvent<StationAiHolderComponent, EntRemovedFromContainerMessage>(OnHolderConRemove);

        SubscribeLocalEvent<StationAiCoreComponent, EntInsertedIntoContainerMessage>(OnAiInsert);
        SubscribeLocalEvent<StationAiCoreComponent, EntRemovedFromContainerMessage>(OnAiRemove);
        SubscribeLocalEvent<StationAiCoreComponent, MapInitEvent>(OnAiMapInit);
        SubscribeLocalEvent<StationAiCoreComponent, ComponentShutdown>(OnAiShutdown);
    }

    public virtual bool SetEnabled(Entity<StationAiVisionComponent> entity, bool enabled, bool announce = false)
    {
        if (entity.Comp.Enabled == enabled)
            return false;

        entity.Comp.Enabled = enabled;
        Dirty(entity);

        return true;
    }

    private void OnAiInteraction(Entity<StationAiHeldComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = !HasComp<StationAiWhitelistComponent>(args.Target);
    }

    private void OnAiAccessible(Entity<StationAiHeldComponent> ent, ref AccessibleOverrideEvent args)
    {
        // TODO: Validate it's near cameras
        args.Accessible = true;
        args.Handled = true;
    }

    private void OnHolderInteract(Entity<StationAiHolderComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp(args.Target, out StationAiHolderComponent? targetHolder))
            return;

        // Try to insert our thing into them
        if (_slots.CanEject(ent.Owner, args.User, ent.Comp.Slot))
        {
            if (!_slots.TryInsert(args.Target.Value, targetHolder.Slot, ent.Comp.Slot.Item!.Value, args.User))
            {
                return;
            }

            args.Handled = true;
            return;
        }

        // Otherwise try to take from them
        if (_slots.CanEject(args.Target.Value, args.User, targetHolder.Slot))
        {
            if (!_slots.TryInsert(ent.Owner, ent.Comp.Slot, targetHolder.Slot.Item!.Value, args.User))
            {
                return;
            }

            args.Handled = true;
        }
    }

    private void OnHolderInit(Entity<StationAiHolderComponent> ent, ref ComponentInit args)
    {
        _slots.AddItemSlot(ent.Owner, StationAiCoreComponent.Container, ent.Comp.Slot);
    }

    private void OnHolderRemove(Entity<StationAiHolderComponent> ent, ref ComponentRemove args)
    {
        _slots.RemoveItemSlot(ent.Owner, ent.Comp.Slot);
    }

    private void OnHolderConInsert(Entity<StationAiHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance((ent.Owner, ent.Comp));
    }

    private void OnHolderConRemove(Entity<StationAiHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateAppearance((ent.Owner, ent.Comp));
    }

    private void OnHolderMapInit(Entity<StationAiHolderComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent.Owner);
    }

    private void OnAiShutdown(Entity<StationAiCoreComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.RemoteEntity);
        ent.Comp.RemoteEntity = null;
    }

    private void OnAiMapInit(Entity<StationAiCoreComponent> ent, ref MapInitEvent args)
    {
        SetupEye(ent);
        AttachEye(ent);
    }

    private void SetupEye(Entity<StationAiCoreComponent> ent)
    {
        if (ent.Comp.RemoteEntityProto != null)
        {
            ent.Comp.RemoteEntity = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(ent.Owner).Coordinates);
        }
    }

    private void AttachEye(Entity<StationAiCoreComponent> ent)
    {
        if (ent.Comp.RemoteEntity == null)
            return;

        if (!_containers.TryGetContainer(ent.Owner, StationAiCoreComponent.Container, out var container) ||
            container.ContainedEntities.Count != 1)
        {
            return;
        }

        var user = container.ContainedEntities[0];

        if (TryComp(user, out EyeComponent? eyeComp))
        {
            _eye.SetTarget(user, ent.Comp.RemoteEntity.Value, eyeComp);
        }

        _mover.SetRelay(user, ent.Comp.RemoteEntity.Value);
    }

    private void OnAiInsert(Entity<StationAiCoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        // Just so text and the likes works properly
        _metadata.SetEntityName(ent.Owner, MetaData(args.Entity).EntityName);

        EnsureComp<IgnoreUIRangeComponent>(args.Entity);
        EnsureComp<StationAiHeldComponent>(args.Entity);
        EnsureComp<StationAiOverlayComponent>(args.Entity);
        EnsureComp<RemoteInteractComponent>(args.Entity);

        AttachEye(ent);
    }

    private void OnAiRemove(Entity<StationAiCoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        // Reset name to whatever
        _metadata.SetEntityName(ent.Owner, Prototype(ent.Owner)?.Name ?? string.Empty);

        // Remove eye relay
        RemCompDeferred<RelayInputMoverComponent>(args.Entity);

        if (TryComp(args.Entity, out EyeComponent? eyeComp))
        {
            _eye.SetTarget(args.Entity, null, eyeComp);
        }

        RemCompDeferred<IgnoreUIRangeComponent>(args.Entity);
        RemCompDeferred<StationAiHeldComponent>(args.Entity);
        RemCompDeferred<StationAiOverlayComponent>(args.Entity);
        RemCompDeferred<RemoteInteractComponent>(args.Entity);
    }

    protected void UpdateAppearance(Entity<StationAiHolderComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!_containers.TryGetContainer(entity.Owner, StationAiCoreComponent.Container, out var container) ||
            container.Count == 0)
        {
            _appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Empty);
            return;
        }

        _appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Occupied);
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
