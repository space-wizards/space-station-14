using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Wires;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles logic, interactions, and UI related to <see cref="BorgChassisComponent"/> and other related components.
/// </summary>
public abstract partial class SharedBorgSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeMMI();
        InitializeModule();
        InitializeRelay();
        InitializeUI();

        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);

        SubscribeLocalEvent<BorgChassisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgChassisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgChassisComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<BorgChassisComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<BorgChassisComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BorgChassisComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<BorgChassisComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BorgChassisComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
        SubscribeLocalEvent<BorgChassisComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<BorgChassisComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<BorgChassisComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BorgChassisComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<BorgChassisComponent, GetCharactedDeadIcEvent>(OnGetDeadIC);
        SubscribeLocalEvent<BorgChassisComponent, GetCharacterUnrevivableIcEvent>(OnGetUnrevivableIC);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        SubscribeLocalEvent<BorgBrainComponent, MindAddedMessage>(OnBrainMindAdded);
        SubscribeLocalEvent<BorgBrainComponent, PointAttemptEvent>(OnBrainPointAttempt);

    }

    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        // TODO: Why the hell is this only broadcasted and not raised directed on the entity?
        // This is doing a ton of HasComps/TryComps.
        if (!HasComp<BorgChassisComponent>(args.ForActor))
        {
            return;
        }

        args.Title = Name(args.ForActor).Trim();
        args.Handled = true;
    }

    private void OnStartup(Entity<BorgChassisComponent> chassis, ref ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(chassis, out var containerManager))
            return;

        chassis.Comp.BrainContainer = _container.EnsureContainer<ContainerSlot>(chassis.Owner, chassis.Comp.BrainContainerId, containerManager);
        chassis.Comp.ModuleContainer = _container.EnsureContainer<Container>(chassis.Owner, chassis.Comp.ModuleContainerId, containerManager);
    }

    private void OnMapInit(Entity<BorgChassisComponent> chassis, ref MapInitEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(chassis.Owner);
    }

    private void OnItemSlotInsertAttempt(Entity<BorgChassisComponent> chassis, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(chassis, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(chassis, out var panelComp))
            return;

        if (!_itemSlots.TryGetSlot(chassis.Owner, cellSlotComp.CellSlotId, out var cellSlot) || cellSlot != args.Slot)
            return;

        if (!panelComp.Open || args.User == chassis.Owner)
            args.Cancelled = true;
    }

    private void OnItemSlotEjectAttempt(Entity<BorgChassisComponent> chassis, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(chassis, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(chassis, out var panel))
            return;

        if (!_itemSlots.TryGetSlot(chassis.Owner, cellSlotComp.CellSlotId, out var cellSlot) || cellSlot != args.Slot)
            return;

        if (!panel.Open || args.User == chassis.Owner)
            args.Cancelled = true;
    }

    // TODO: consider transferring over the ghost role? managing that might suck.
    protected virtual void OnInserted(Entity<BorgChassisComponent> chassis, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (args.Container != chassis.Comp.BrainContainer)
            return;

        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(args.Entity, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, chassis.Owner, mind: mind);
        }
    }

    protected virtual void OnRemoved(Entity<BorgChassisComponent> chassis, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (args.Container != chassis.Comp.BrainContainer)
            return;

        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(chassis.Owner, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, args.Entity, mind: mind);
        }
    }

    private void OnMindAdded(Entity<BorgChassisComponent> chassis, ref MindAddedMessage args)
    {
        // Unpredicted because the event is raised on the server.
        _popup.PopupEntity(Loc.GetString("borg-mind-added", ("name", Identity.Name(chassis.Owner, EntityManager))), chassis.Owner);

        TryActivate(chassis);

        _appearance.SetData(chassis.Owner, BorgVisuals.HasPlayer, true);
    }

    private void OnMindRemoved(Entity<BorgChassisComponent> chassis, ref MindRemovedMessage args)
    {
        // Unpredicted because the event is raised on the server.
        _popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(chassis.Owner, EntityManager))), chassis.Owner);

        SetActive(chassis, false);
        // Turn off the light so that the no-player visuals can be seen.
        if (TryComp<HandheldLightComponent>(chassis.Owner, out var light))
            _handheldLight.TurnOff((chassis.Owner, light), makeNoise: false); // Already plays a sound when toggling the borg off.
        _appearance.SetData(chassis.Owner, BorgVisuals.HasPlayer, false);
    }

    private void OnChassisInteractUsing(Entity<BorgChassisComponent> chassis, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Handled || chassis.Owner == args.User)
            return;

        var used = args.Used;
        TryComp<BorgBrainComponent>(used, out var brain);
        TryComp<BorgModuleComponent>(used, out var module);

        if (TryComp<WiresPanelComponent>(chassis, out var panel) && !panel.Open)
        {
            if (brain != null || module != null)
            {
                _popup.PopupClient(Loc.GetString("borg-panel-not-open"), chassis, args.User);
            }
            return;
        }

        if (chassis.Comp.BrainEntity == null && brain != null &&
            _whitelist.IsWhitelistPassOrNull(chassis.Comp.BrainWhitelist, used))
        {
            if (TryComp<ActorComponent>(used, out var actor) && !CanPlayerBeBorged(actor.PlayerSession))
            {
                // Don't use PopupClient because CanPlayerBeBorged is not predicted.
                _popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), used, args.User);
                return;
            }

            _container.Insert(used, chassis.Comp.BrainContainer);
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{args.User} installed brain {used} into borg {chassis.Owner}");
            args.Handled = true;
            return;
        }

        if (module != null && CanInsertModule(chassis.AsNullable(), (used, module), args.User))
        {
            InsertModule(chassis, used);
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{args.User} installed module {used} into borg {chassis.Owner}");
            args.Handled = true;
        }
    }

    // Make the borg slower without power.
    private void OnRefreshMovementSpeedModifiers(Entity<BorgChassisComponent> chassis, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (chassis.Comp.Active)
            return;

        if (!TryComp<MovementSpeedModifierComponent>(chassis, out var movement))
            return;

        if (movement.BaseSprintSpeed == 0f)
            return; // We already cannot move.

        // Slow down to walk speed.
        var sprintDif = movement.BaseWalkSpeed / movement.BaseSprintSpeed;
        args.ModifySpeed(1f, sprintDif);
    }

    private void OnUIOpenAttempt(Entity<BorgChassisComponent> chassis, ref ActivatableUIOpenAttemptEvent args)
    {
        // Borgs generally can't view their own UI.
        if (args.User == chassis.Owner && !chassis.Comp.CanOpenSelfUi)
            args.Cancel();
    }

    private void OnMobStateChanged(Entity<BorgChassisComponent> chassis, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            TryActivate(chassis, args.Origin);
        else
            SetActive(chassis, false, user: args.Origin);
    }

    private void OnBeingGibbed(Entity<BorgChassisComponent> chassis, ref BeingGibbedEvent args)
    {
        // Don't use the ItemSlotsSystem eject method since we don't want to play a sound and want we to eject the battery even if the slot is locked.
        if (TryComp<PowerCellSlotComponent>(chassis, out var slotComp) &&
            _container.TryGetContainer(chassis, slotComp.CellSlotId, out var slotContainer))
            _container.EmptyContainer(slotContainer);

        _container.EmptyContainer(chassis.Comp.BrainContainer);
        _container.EmptyContainer(chassis.Comp.ModuleContainer);
    }

    private void OnGetDeadIC(Entity<BorgChassisComponent> chassis, ref GetCharactedDeadIcEvent args)
    {
        args.Dead = true;
    }

    private void OnGetUnrevivableIC(Entity<BorgChassisComponent> chassis, ref GetCharacterUnrevivableIcEvent args)
    {
        args.Unrevivable = true;
    }

    private void OnBrainMindAdded(Entity<BorgBrainComponent> brain, ref MindAddedMessage args)
    {
        if (!_container.TryGetContainingContainer(brain.Owner, out var container))
            return;

        var borg = container.Owner;

        if (!TryComp<BorgChassisComponent>(borg, out var chassisComponent) ||
            container.ID != chassisComponent.BrainContainerId)
            return;

        if (!_mind.TryGetMind(brain.Owner, out var mindId, out var mind) ||
            !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        if (!CanPlayerBeBorged(session))
        {
            // Don't use PopupClient because MindAddedMessage and CanPlayerBeBorged are not predicted.
            _popup.PopupEntity(Loc.GetString("borg-player-not-allowed-eject"), brain);
            _container.RemoveEntity(borg, brain);
            _throwing.TryThrow(brain, _random.NextVector2() * 5, 5f);
            return;
        }

        _mind.TransferTo(mindId, borg, mind: mind);
    }

    private void OnBrainPointAttempt(Entity<BorgBrainComponent> brain, ref PointAttemptEvent args)
    {
        args.Cancel();
    }

    // Raised when the power cell is empty or removed from the borg.
    private void OnPowerCellSlotEmpty(Entity<BorgChassisComponent> chassis, ref PowerCellSlotEmptyEvent args)
    {
        SetActive(chassis, false);
    }

    // Raised when a power cell is inserted.
    private void OnPowerCellChanged(Entity<BorgChassisComponent> chassis, ref PowerCellChangedEvent args)
    {
        TryActivate(chassis);
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BorgChassisComponent>();
        while (query.MoveNext(out var uid, out var borgChassis))
        {
            if (curTime < borgChassis.NextBatteryUpdate)
                continue;

            borgChassis.NextBatteryUpdate = curTime + TimeSpan.FromSeconds(1);
            Dirty(uid, borgChassis);

            // If we aren't drawing and suddenly get enough power to draw again, reenable.
            TryActivate((uid, borgChassis));
        }
    }
}
