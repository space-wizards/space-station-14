using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Administration.Managers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem : EntitySystem
{
    [Dependency] private readonly   ISharedAdminManager _admin = default!;
    [Dependency] private readonly   IGameTiming _timing = default!;
    [Dependency] private readonly   INetManager _net = default!;
    [Dependency] private readonly   ItemSlotsSystem _slots = default!;
    [Dependency] private readonly   ItemToggleSystem _toggles = default!;
    [Dependency] private readonly   ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly   MetaDataSystem _metadata = default!;
    [Dependency] private readonly   SharedAirlockSystem _airlocks = default!;
    [Dependency] private readonly   SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly   SharedAudioSystem _audio = default!;
    [Dependency] private readonly   SharedContainerSystem _containers = default!;
    [Dependency] private readonly   SharedDoorSystem _doors = default!;
    [Dependency] private readonly   SharedElectrocutionSystem _electrify = default!;
    [Dependency] private readonly   SharedEyeSystem _eye = default!;
    [Dependency] protected readonly SharedMapSystem Maps = default!;
    [Dependency] private readonly   SharedMindSystem _mind = default!;
    [Dependency] private readonly   SharedMoverController _mover = default!;
    [Dependency] private readonly   SharedPopupSystem _popup = default!;
    [Dependency] private readonly   SharedPowerReceiverSystem PowerReceiver = default!;
    [Dependency] private readonly   SharedTransformSystem _xforms = default!;
    [Dependency] private readonly   SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly   StationAiVisionSystem _vision = default!;

    // StationAiHeld is added to anything inside of an AI core.
    // StationAiHolder indicates it can hold an AI positronic brain (e.g. holocard / core).
    // StationAiCore holds functionality related to the core itself.
    // StationAiWhitelist is a general whitelist to stop it being able to interact with anything
    // StationAiOverlay handles the static overlay. It also handles interaction blocking on client and server
    // for anything under it.

    private EntityQuery<BroadphaseComponent> _broadphaseQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    [ValidatePrototypeId<EntityPrototype>]
    private static readonly EntProtoId DefaultAi = "StationAiBrain";

    public override void Initialize()
    {
        base.Initialize();

        _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        InitializeAirlock();
        InitializeHeld();
        InitializeLight();

        SubscribeLocalEvent<StationAiWhitelistComponent, BoundUserInterfaceCheckRangeEvent>(OnAiBuiCheck);

        SubscribeLocalEvent<StationAiOverlayComponent, AccessibleOverrideEvent>(OnAiAccessible);
        SubscribeLocalEvent<StationAiOverlayComponent, InRangeOverrideEvent>(OnAiInRange);
        SubscribeLocalEvent<StationAiOverlayComponent, MenuVisibilityEvent>(OnAiMenu);

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
        SubscribeLocalEvent<StationAiCoreComponent, PowerChangedEvent>(OnCorePower);
        SubscribeLocalEvent<StationAiCoreComponent, GetVerbsEvent<Verb>>(OnCoreVerbs);
    }

    private void OnCoreVerbs(Entity<StationAiCoreComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.IsAdmin(args.User) ||
            TryGetHeld((ent.Owner, ent.Comp), out _))
        {
            return;
        }

        var user = args.User;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("station-ai-takeover"),
            Category = VerbCategory.Debug,
            Act = () =>
            {
                var brain = SpawnInContainerOrDrop(DefaultAi, ent.Owner, StationAiCoreComponent.Container);
                _mind.ControlMob(user, brain);
            },
            Impact = LogImpact.High,
        });
    }

    private void OnAiAccessible(Entity<StationAiOverlayComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;

        // Hopefully AI never needs storage
        if (_containers.TryGetContainingContainer(args.Target, out var targetContainer))
        {
            return;
        }

        if (!_containers.IsInSameOrTransparentContainer(args.User, args.Target, otherContainer: targetContainer))
        {
            return;
        }

        args.Accessible = true;
    }

    private void OnAiMenu(Entity<StationAiOverlayComponent> ent, ref MenuVisibilityEvent args)
    {
        args.Visibility &= ~MenuVisibility.NoFov;
    }

    private void OnAiBuiCheck(Entity<StationAiWhitelistComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (!HasComp<StationAiHeldComponent>(args.Actor))
            return;

        args.Result = BoundUserInterfaceRangeResult.Fail;

        // Similar to the inrange check but more optimised so server doesn't die.
        var targetXform = Transform(args.Target);

        // No cross-grid
        if (targetXform.GridUid != args.Actor.Comp.GridUid)
        {
            return;
        }

        if (!_broadphaseQuery.TryComp(targetXform.GridUid, out var broadphase) || !_gridQuery.TryComp(targetXform.GridUid, out var grid))
        {
            return;
        }

        var targetTile = Maps.LocalToTile(targetXform.GridUid.Value, grid, targetXform.Coordinates);

        lock (_vision)
        {
            if (_vision.IsAccessible((targetXform.GridUid.Value, broadphase, grid), targetTile, fastPath: true))
            {
                args.Result = BoundUserInterfaceRangeResult.Pass;
            }
        }
    }

    private void OnAiInRange(Entity<StationAiOverlayComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Handled = true;
        var targetXform = Transform(args.Target);

        // No cross-grid
        if (targetXform.GridUid != Transform(args.User).GridUid)
        {
            return;
        }

        // Validate it's in camera range yes this is expensive.
        // Yes it needs optimising
        if (!_broadphaseQuery.TryComp(targetXform.GridUid, out var broadphase) || !_gridQuery.TryComp(targetXform.GridUid, out var grid))
        {
            return;
        }

        var targetTile = Maps.LocalToTile(targetXform.GridUid.Value, grid, targetXform.Coordinates);

        args.InRange = _vision.IsAccessible((targetXform.GridUid.Value, broadphase, grid), targetTile);
    }

    private void OnHolderInteract(Entity<StationAiHolderComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp(args.Target, out StationAiHolderComponent? targetHolder))
            return;

        // Try to insert our thing into them
        if (_slots.CanEject(ent.Owner, args.User, ent.Comp.Slot))
        {
            if (!_slots.TryInsert(args.Target.Value, targetHolder.Slot, ent.Comp.Slot.Item!.Value, args.User, excludeUserAudio: true))
            {
                return;
            }

            args.Handled = true;
            return;
        }

        // Otherwise try to take from them
        if (_slots.CanEject(args.Target.Value, args.User, targetHolder.Slot))
        {
            if (!_slots.TryInsert(ent.Owner, ent.Comp.Slot, targetHolder.Slot.Item!.Value, args.User, excludeUserAudio: true))
            {
                return;
            }

            args.Handled = true;
        }
    }

    private void OnHolderInit(Entity<StationAiHolderComponent> ent, ref ComponentInit args)
    {
        _slots.AddItemSlot(ent.Owner, StationAiHolderComponent.Container, ent.Comp.Slot);
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
        // TODO: Tryqueuedel
        if (_net.IsClient)
            return;

        QueueDel(ent.Comp.RemoteEntity);
        ent.Comp.RemoteEntity = null;
    }

    private void OnCorePower(Entity<StationAiCoreComponent> ent, ref PowerChangedEvent args)
    {
        // TODO: I think in 13 they just straightup die so maybe implement that
        if (args.Powered)
        {
            if (!SetupEye(ent))
                return;

            AttachEye(ent);
        }
        else
        {
            ClearEye(ent);
        }
    }

    private void OnAiMapInit(Entity<StationAiCoreComponent> ent, ref MapInitEvent args)
    {
        SetupEye(ent);
        AttachEye(ent);
    }

    private bool SetupEye(Entity<StationAiCoreComponent> ent)
    {
        if (_net.IsClient)
            return false;
        if (ent.Comp.RemoteEntity != null)
            return false;

        if (ent.Comp.RemoteEntityProto != null)
        {
            ent.Comp.RemoteEntity = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(ent.Owner).Coordinates);
            Dirty(ent);
        }

        return true;
    }

    private void ClearEye(Entity<StationAiCoreComponent> ent)
    {
        if (_net.IsClient)
            return;
        QueueDel(ent.Comp.RemoteEntity);
        ent.Comp.RemoteEntity = null;
        Dirty(ent);
    }

    private void AttachEye(Entity<StationAiCoreComponent> ent)
    {
        if (ent.Comp.RemoteEntity == null)
            return;

        if (!_containers.TryGetContainer(ent.Owner, StationAiHolderComponent.Container, out var container) ||
            container.ContainedEntities.Count != 1)
        {
            return;
        }

        // Attach them to the portable eye that can move around.
        var user = container.ContainedEntities[0];

        if (TryComp(user, out EyeComponent? eyeComp))
        {
            _eye.SetDrawFov(user, false, eyeComp);
            _eye.SetTarget(user, ent.Comp.RemoteEntity.Value, eyeComp);
        }

        _mover.SetRelay(user, ent.Comp.RemoteEntity.Value);
    }

    private void OnAiInsert(Entity<StationAiCoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        SetupEye(ent);

        // Just so text and the likes works properly
        _metadata.SetEntityName(ent.Owner, MetaData(args.Entity).EntityName);

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
            _eye.SetDrawFov(args.Entity, true, eyeComp);
            _eye.SetTarget(args.Entity, null, eyeComp);
        }
        ClearEye(ent);
    }

    private void UpdateAppearance(Entity<StationAiHolderComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!_containers.TryGetContainer(entity.Owner, StationAiHolderComponent.Container, out var container) ||
            container.Count == 0)
        {
            _appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Empty);
            return;
        }

        _appearance.SetData(entity.Owner, StationAiVisualState.Key, StationAiState.Occupied);
    }

    public virtual bool SetVisionEnabled(Entity<StationAiVisionComponent> entity, bool enabled, bool announce = false)
    {
        if (entity.Comp.Enabled == enabled)
            return false;

        entity.Comp.Enabled = enabled;
        Dirty(entity);

        return true;
    }

    public virtual bool SetWhitelistEnabled(Entity<StationAiWhitelistComponent> entity, bool value, bool announce = false)
    {
        if (entity.Comp.Enabled == value)
            return false;

        entity.Comp.Enabled = value;
        Dirty(entity);

        return true;
    }

    /// <summary>
    /// BUI validation for ai interactions.
    /// </summary>
    private bool ValidateAi(Entity<StationAiHeldComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
        {
            return false;
        }

        return _blocker.CanComplexInteract(entity.Owner);
    }
}

public sealed partial class JumpToCoreEvent : InstantActionEvent
{

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
