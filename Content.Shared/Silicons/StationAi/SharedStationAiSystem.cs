using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Administration.Managers;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Doors.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Intellicard;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Repairable;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ItemToggleSystem _toggles = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedAirlockSystem _airlocks = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedDoorSystem _doors = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrify = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] protected readonly SharedMapSystem Maps = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem PowerReceiver = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly StationAiVisionSystem _vision = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    // StationAiHeld is added to anything inside of an AI core.
    // StationAiHolder indicates it can hold an AI positronic brain (e.g. holocard / core).
    // StationAiCore holds functionality related to the core itself.
    // StationAiWhitelist is a general whitelist to stop it being able to interact with anything
    // StationAiOverlay handles the static overlay. It also handles interaction blocking on client and server
    // for anything under it.

    private EntityQuery<BroadphaseComponent> _broadphaseQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    private static readonly EntProtoId DefaultAi = "StationAiBrain";
    private readonly ProtoId<ChatNotificationPrototype> _downloadChatNotificationPrototype = "IntellicardDownload";

    public override void Initialize()
    {
        base.Initialize();

        _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        InitializeAirlock();
        InitializeHeld();
        InitializeLight();
        InitializeCustomization();

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
        SubscribeLocalEvent<StationAiHolderComponent, IntellicardDoAfterEvent>(OnIntellicardDoAfter);

        SubscribeLocalEvent<StationAiCoreComponent, EntInsertedIntoContainerMessage>(OnAiInsert);
        SubscribeLocalEvent<StationAiCoreComponent, EntRemovedFromContainerMessage>(OnAiRemove);
        SubscribeLocalEvent<StationAiCoreComponent, ComponentShutdown>(OnAiShutdown);
        SubscribeLocalEvent<StationAiCoreComponent, PowerChangedEvent>(OnCorePower);
        SubscribeLocalEvent<StationAiCoreComponent, GetVerbsEvent<Verb>>(OnCoreVerbs);

        SubscribeLocalEvent<StationAiCoreComponent, BreakageEventArgs>(OnBroken);
        SubscribeLocalEvent<StationAiCoreComponent, RepairedEvent>(OnRepaired);
    }

    private void OnCoreVerbs(Entity<StationAiCoreComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        // Admin option to take over the station AI core
        if (_admin.IsAdmin(args.User) &&
            !TryGetHeld((ent.Owner, ent.Comp), out _))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("station-ai-takeover"),
                Category = VerbCategory.Debug,
                Act = () =>
                {
                    if (_net.IsClient)
                        return;
                    var brain = SpawnInContainerOrDrop(DefaultAi, ent.Owner, StationAiCoreComponent.Container);
                    _mind.ControlMob(user, brain);
                },
                Impact = LogImpact.High,
            });
        }

        // Option to open the station AI customization menu
        if (TryGetHeld((ent, ent.Comp), out var insertedAi) && insertedAi == user)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("station-ai-customization-menu"),
                Act = () => _uiSystem.TryOpenUi(ent.Owner, StationAiCustomizationUiKey.Key, insertedAi.Value),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/emotes.svg.192dpi.png")),
            });
        }
    }

    private void OnAiAccessible(Entity<StationAiOverlayComponent> ent, ref AccessibleOverrideEvent args)
    {
        // We don't want to allow entities to access the AI just because the eye is nearby.
        // Only let the AI access entities through the eye.
        if (args.Accessible || args.User != ent.Owner)
            return;

        args.Handled = true;

        // Hopefully AI never needs storage
        if (_containers.TryGetContainingContainer(args.Target, out var targetContainer) ||
            !_containers.IsInSameOrTransparentContainer(ent.Owner, args.Target, otherContainer: targetContainer))
            return;

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


    private void OnIntellicardDoAfter(Entity<StationAiHolderComponent> ent, ref IntellicardDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(args.Args.Target, out StationAiHolderComponent? targetHolder))
            return;

        // Try to insert our thing into them
        if (_slots.CanEject(ent.Owner, args.User, ent.Comp.Slot))
        {
            if (!_slots.TryInsert(args.Args.Target.Value, targetHolder.Slot, ent.Comp.Slot.Item!.Value, args.User, excludeUserAudio: true))
            {
                return;
            }

            args.Handled = true;
            return;
        }

        // Otherwise try to take from them
        if (_slots.CanEject(args.Args.Target.Value, args.User, targetHolder.Slot))
        {
            if (!_slots.TryInsert(ent.Owner, ent.Comp.Slot, targetHolder.Slot.Item!.Value, args.User, excludeUserAudio: true))
            {
                return;
            }

            args.Handled = true;
        }
    }

    private void OnHolderInteract(Entity<StationAiHolderComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp(args.Target, out StationAiHolderComponent? targetHolder))
            return;

        //Don't want to download/upload between several intellicards. You can just pick it up at that point.
        if (HasComp<IntellicardComponent>(args.Target))
            return;

        if (!TryComp(args.Used, out IntellicardComponent? intelliComp))
            return;

        var cardHasAi = ent.Comp.Slot.Item != null;
        var coreHasAi = targetHolder.Slot.Item != null;

        if (cardHasAi && coreHasAi)
        {
            _popup.PopupClient(Loc.GetString("intellicard-core-occupied"), args.User, args.User, PopupType.Medium);
            args.Handled = true;
            return;
        }
        if (!cardHasAi && !coreHasAi)
        {
            _popup.PopupClient(Loc.GetString("intellicard-core-empty"), args.User, args.User, PopupType.Medium);
            args.Handled = true;
            return;
        }

        if (TryGetHeld((args.Target.Value, targetHolder), out var held))
        {
            var ev = new ChatNotificationEvent(_downloadChatNotificationPrototype, args.Used, args.User);
            RaiseLocalEvent(held.Value, ref ev);
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, cardHasAi ? intelliComp.UploadTime : intelliComp.DownloadTime, new IntellicardDoAfterEvent(), args.Target, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
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
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.Slot.ID)
            return;

        UpdateAppearance((ent.Owner, ent.Comp));

        if (ent.Comp.RenameOnInsert)
            _metadata.SetEntityName(ent.Owner, MetaData(args.Entity).EntityName);
    }

    private void OnHolderConRemove(Entity<StationAiHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.Slot.ID)
            return;

        UpdateAppearance((ent.Owner, ent.Comp));

        if (ent.Comp.RenameOnInsert)
            _metadata.SetEntityName(ent.Owner, Prototype(ent.Owner)?.Name ?? string.Empty);
    }

    private void OnHolderMapInit(Entity<StationAiHolderComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance((ent.Owner, ent.Comp));
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
        if (!args.Powered)
        {
            KillHeldAi(ent);
        }
    }

    private void OnBroken(Entity<StationAiCoreComponent> ent, ref BreakageEventArgs args)
    {
        KillHeldAi(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, StationAiVisuals.Broken, true, appearance);
    }

    private void OnRepaired(Entity<StationAiCoreComponent> ent, ref RepairedEvent args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, StationAiVisuals.Broken, false, appearance);
    }

    public virtual void KillHeldAi(Entity<StationAiCoreComponent> ent)
    {
        if (TryGetHeld((ent.Owner, ent.Comp), out var held))
        {
            _mobState.ChangeMobState(held.Value, MobState.Dead);
        }
    }

    public void SwitchRemoteEntityMode(Entity<StationAiCoreComponent?> entity, bool isRemote)
    {
        if (entity.Comp?.Remote == null || entity.Comp.Remote == isRemote)
            return;

        var ent = new Entity<StationAiCoreComponent>(entity.Owner, entity.Comp);

        ent.Comp.Remote = isRemote;

        EntityCoordinates? coords = ent.Comp.RemoteEntity != null ? Transform(ent.Comp.RemoteEntity.Value).Coordinates : null;

        // Attach new eye
        var oldEye = ent.Comp.RemoteEntity;

        ClearEye(ent);

        if (SetupEye(ent, coords))
            AttachEye(ent);

        if (oldEye != null)
        {
            // Raise the following event on the old eye before it's deleted
            var ev = new StationAiRemoteEntityReplacementEvent(ent.Comp.RemoteEntity);
            RaiseLocalEvent(oldEye.Value, ref ev);
        }

        // Adjust user FoV
        var user = GetInsertedAI(ent);

        if (TryComp<EyeComponent>(user, out var eye))
            _eye.SetDrawFov(user.Value, !isRemote);
    }

    protected bool SetupEye(Entity<StationAiCoreComponent> ent, EntityCoordinates? coords = null)
    {
        if (_net.IsClient)
            return false;

        if (ent.Comp.RemoteEntity != null)
            return false;

        var proto = ent.Comp.RemoteEntityProto;

        if (coords == null)
            coords = Transform(ent.Owner).Coordinates;

        if (!ent.Comp.Remote)
            proto = ent.Comp.PhysicalEntityProto;

        if (proto != null)
        {
            ent.Comp.RemoteEntity = SpawnAtPosition(proto, coords.Value);
            Dirty(ent);
        }

        return true;
    }

    protected void ClearEye(Entity<StationAiCoreComponent> ent)
    {
        if (_net.IsClient)
            return;

        QueueDel(ent.Comp.RemoteEntity);
        ent.Comp.RemoteEntity = null;
        Dirty(ent);

        if (TryGetHeld((ent, ent.Comp), out var held) &&
            TryComp(held, out EyeComponent? eyeComp))
        {
            _eye.SetDrawFov(held.Value, true, eyeComp);
            _eye.SetTarget(held.Value, null, eyeComp);
        }
    }

    protected void AttachEye(Entity<StationAiCoreComponent> ent)
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

        var eyeName = Loc.GetString("station-ai-eye-name", ("name", Name(user)));
        _metadata.SetEntityName(ent.Comp.RemoteEntity.Value, eyeName);
    }

    private EntityUid? GetInsertedAI(Entity<StationAiCoreComponent> ent)
    {
        if (!_containers.TryGetContainer(ent.Owner, StationAiHolderComponent.Container, out var container) ||
            container.ContainedEntities.Count != 1)
        {
            return null;
        }

        return container.ContainedEntities[0];
    }

    protected virtual void OnAiInsert(Entity<StationAiCoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != StationAiCoreComponent.Container)
            return;

        if (_timing.ApplyingState)
            return;

        ClearEye(ent);
        ent.Comp.Remote = true;

        if (SetupEye(ent))
            AttachEye(ent);
    }

    protected virtual void OnAiRemove(Entity<StationAiCoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StationAiCoreComponent.Container)
            return;

        if (_timing.ApplyingState)
            return;

        ent.Comp.Remote = true;

        // Remove eye relay
        RemCompDeferred<RelayInputMoverComponent>(args.Entity);

        if (TryComp(args.Entity, out EyeComponent? eyeComp))
        {
            _eye.SetDrawFov(args.Entity, true, eyeComp);
            _eye.SetTarget(args.Entity, null, eyeComp);
        }

        ClearEye(ent);
    }

    protected void UpdateAppearance(Entity<StationAiHolderComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        var state = StationAiState.Empty;

        // Get what visual state the held AI holder is in
        if (TryGetHeld(entity, out var stationAi) &&
            TryComp<StationAiCustomizationComponent>(stationAi, out var customization))
        {
            state = customization.State;
        }

        // If the entity is not an AI core, let generic visualizers handle the appearance update
        if (!TryComp<StationAiCoreComponent>(entity, out var stationAiCore))
        {
            _appearance.SetData(entity.Owner, StationAiVisualLayers.Icon, state);
            return;
        }

        // The AI core is empty
        if (state == StationAiState.Empty)
        {
            _appearance.RemoveData(entity.Owner, StationAiVisualLayers.Icon);
            return;
        }

        // The AI core is rebooting
        if (state == StationAiState.Rebooting)
        {
            var rebootingData = new PrototypeLayerData()
            {
                RsiPath = _stationAiRebooting.RsiPath.ToString(),
                State = _stationAiRebooting.RsiState,
            };

            _appearance.SetData(entity.Owner, StationAiVisualLayers.Icon, rebootingData);
            return;
        }

        // Otherwise attempt to set the AI core's appearance
        CustomizeAppearance((entity, stationAiCore), state);
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
public sealed partial class IntellicardDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public enum StationAiVisualLayers : byte
{
    Base,
    Icon,
}

[Serializable, NetSerializable]
public enum StationAiVisuals : byte
{
    Broken,
}

[Serializable, NetSerializable]
public enum StationAiState : byte
{
    Empty,
    Occupied,
    Dead,
    Rebooting,
    Hologram,
}
