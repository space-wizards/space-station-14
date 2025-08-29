using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Body.Events;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Content.Shared.Light.Components;
using Content.Shared.Actions.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Maths;
using Content.Shared.Repairable.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Handles all of the interactions, UI handling, and items shennanigans for <see cref="MechComponent"/>
/// </summary>
public abstract partial class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        SubscribeLocalEvent<MechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
        SubscribeLocalEvent<MechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<MechComponent, EntityStorageIntoContainerAttemptEvent>(OnEntityStorageDump);
        SubscribeLocalEvent<MechComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<MechComponent, CanDropTargetEvent>(OnCanDragDrop);
        SubscribeLocalEvent<MechComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<Verb>>(OnGetVerb);

        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetActiveWeaponEvent>(OnGetActiveWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetShootingEntityEvent>(OnGetShootingEntity);
        SubscribeLocalEvent<MechPilotComponent, GetProjectileShooterEvent>(OnGetProjectileShooter);

        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnCanAttackFromContainer);
        SubscribeLocalEvent<MechPilotComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<MechComponent, RepairAttemptEvent>(OnRepairAttempt);

        SubscribeLocalEvent<MechEquipmentComponent, ShotAttemptedEvent>(OnMechEquipmentShotAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, AttemptMeleeEvent>(OnMechEquipmentMeleeAttempt);

        SubscribeLocalEvent<MechPilotComponent, AccessibleOverrideEvent>(OnPilotAccessible);
        SubscribeLocalEvent<MechEquipmentComponent, GettingUsedAttemptEvent>(OnMechEquipmentGettingUsedAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, ActivatableUIOpenAttemptEvent>(OnMechEquipmentUiOpenAttempt);
        SubscribeLocalEvent<MechPilotComponent, GetUsedEntityEvent>(OnPilotGetUsedEntity);
        SubscribeLocalEvent<MechComponent, UseHeldBypassAttemptEvent>(OnUseHeldBypass);

        InitializeRelay();
    }

    private void OnToggleEquipmentAction(EntityUid uid, MechComponent component, MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsServer)
        {
            args.Handled = true;
        }
        else
        {
            RaiseLocalEvent(uid, new MechOpenEquipmentRadialEvent());
            args.Handled = true;
        }
    }

    private void OnEjectPilotEvent(EntityUid uid, MechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, component.EntryDelay, new MechExitEvent(), uid, target: uid)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnStartup(EntityUid uid, MechComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
        component.EquipmentContainer = _container.EnsureContainer<Container>(uid, component.EquipmentContainerId);
        component.BatterySlot = _container.EnsureContainer<ContainerSlot>(uid, component.BatterySlotId);
        component.ModuleContainer = _container.EnsureContainer<Container>(uid, component.ModuleContainerId);
    }

    private void OnDestruction(EntityUid uid, MechComponent component, DestructionEventArgs args)
    {
        TryEject(uid, component);
        UpdateAppearance(uid, component);
    }

    private void OnEntityStorageDump(Entity<MechComponent> entity, ref EntityStorageIntoContainerAttemptEvent args)
    {
        // There's no reason we should dump into /any/ of the mech's containers.
        args.Cancelled = true;
    }

    private void ManageVirtualItems(EntityUid pilot, EntityUid mech, bool create)
    {
        if (!TryComp<HandsComponent>(pilot, out var handsComp))
            return;

        if (create)
        {
            // Creating virtual items to block hands.
            var blocking = TryComp<MechComponent>(mech, out var mechComp) && mechComp.CurrentSelectedEquipment != null
                ? mechComp.CurrentSelectedEquipment.Value
                : mech;

            foreach (var hand in _hands.EnumerateHands(pilot))
            {
                if (_hands.TryGetHeldItem(pilot, hand, out _))
                    continue;

                if (_virtualItem.TrySpawnVirtualItemInHand(blocking, pilot, out var virtualItem, dropOthers: false))
                {
                    EnsureComp<UnremoveableComponent>(virtualItem.Value);
                }
            }
        }
        else
        {
            // Remove virtual items for mech and equipment.
            _virtualItem.DeleteInHandsMatching(pilot, mech);
            if (TryComp<MechComponent>(mech, out var mechComp))
            {
                foreach (var eq in mechComp.EquipmentContainer.ContainedEntities)
                {
                    _virtualItem.DeleteInHandsMatching(pilot, eq);
                }
            }
        }
    }

    private void SetupUser(EntityUid mech, EntityUid pilot, MechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<MechPilotComponent>(pilot);
        rider.Mech = mech;
        Dirty(pilot, rider);

        if (_net.IsClient)
            return;

        // Drop held items upon entering.
        if (!TryComp<HandsComponent>(pilot, out var handsComp))
            return;

        foreach (var hand in _hands.EnumerateHands(pilot))
        {
            if (_hands.TryGetHeldItem(pilot, hand, out _))
                _hands.TryDrop((pilot, handsComp), hand);
        }

        ManageVirtualItems(pilot, mech, create: true);

        _actions.AddAction(pilot, ref component.MechCycleActionEntity, component.MechCycleAction, mech);
        _actions.AddAction(pilot, ref component.MechUiActionEntity, component.MechUiAction, mech);
        _actions.AddAction(pilot, ref component.MechEjectActionEntity, component.MechEjectAction, mech);
        GrantMechProvidedActions(pilot, mech);
    }

    private void RemoveUser(EntityUid mech, EntityUid pilot)
    {
        RemComp<MechPilotComponent>(pilot);

        ManageVirtualItems(pilot, mech, create: false);

        _actions.RemoveProvidedActions(pilot, mech);
    }

    /// <summary>
    /// Inserts a piece of equipment or a module into a mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <param name="equipmentComponent"></param>
    /// <param name="moduleComponent"></param>
    public void InsertEquipment(EntityUid uid, EntityUid toInsert, MechComponent? component = null,
        MechEquipmentComponent? equipmentComponent = null, MechModuleComponent? moduleComponent = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Broken)
            return;

        // Equipment
        if (Resolve(toInsert, ref equipmentComponent, false))
        {
            if (component.EquipmentContainer.ContainedEntities.Count >= component.MaxEquipmentAmount)
                return;

            if (_whitelistSystem.IsWhitelistFail(component.EquipmentWhitelist, toInsert))
                return;

            equipmentComponent.EquipmentOwner = uid;
            _container.Insert(toInsert, component.EquipmentContainer);
            var ev = new MechEquipmentInsertedEvent(uid);
            RaiseLocalEvent(toInsert, ref ev);
            UpdateUserInterface(uid);
            return;
        }

        // Module
        if (Resolve(toInsert, ref moduleComponent, false))
        {
            if (component.ModuleContainer.ContainedEntities.Count >= component.MaxModuleAmount)
                return;

            if (_whitelistSystem.IsWhitelistFail(component.ModuleWhitelist, toInsert))
                return;

            moduleComponent.ModuleOwner = uid;
            _container.Insert(toInsert, component.ModuleContainer);
            var modEv = new MechModuleInsertedEvent(uid);
            RaiseLocalEvent(toInsert, ref modEv);
            UpdateUserInterface(uid);
            return;
        }
    }

    /// <summary>
    /// Updates the pilot's virtual items in their hands to visually match the selected equipment.
    /// </summary>
    public void RefreshPilotHandVirtualItems(EntityUid mech, MechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var pilot = Vehicle.GetOperatorOrNull(mech);
        if (pilot == null)
            return;

        foreach (var held in _hands.EnumerateHeld(pilot.Value))
        {
            if (!TryComp<VirtualItemComponent>(held, out var virt))
                continue;

            var newBlocking = component.CurrentSelectedEquipment ?? mech;
            if (virt.BlockingEntity != newBlocking)
            {
                virt.BlockingEntity = newBlocking;
                Dirty(held, virt);
            }
        }
    }


    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="value">The value the integrity will be set at</param>
    /// <param name="component"></param>
    public void SetIntegrity(EntityUid uid, FixedPoint2 value, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        // Handle broken state transitions based on integrity
        if (component.Integrity <= 0)
        {
            // If already in broken state, check if should be gibbed
            if (component.Broken)
            {
                if (component.Integrity < -component.BrokenThreshold)
                {
                    var gibEvent = new BeingGibbedEvent(new HashSet<EntityUid>());
                    RaiseLocalEvent(uid, ref gibEvent);
                    return;
                }
            }
            else
            {
                SetBrokenState(uid, component);
            }
        }
        else if (component.Integrity > component.BrokenThreshold && component.Broken)
        {
            component.Broken = false;
        }

        Dirty(uid, component);
        UpdateUserInterface(uid);
        UpdateAppearance(uid, component);
    }

    /// <summary>
    /// Throws an entity away from the mech in a random direction at a random speed.
    /// </summary>
    private void ScatterEntityFromMech(EntityUid mech, EntityUid ent, float minSpeed = 4f, float maxSpeed = 7f)
    {
        var direction = _random.NextAngle().ToWorldVec();
        var speed = _random.NextFloat(minSpeed, maxSpeed);
        _throwing.TryThrow(ent, direction, speed);
    }

    /// <summary>
    /// Sets the mech to broken state (destroyed but can be repaired).
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="component"></param>
    public void SetBrokenState(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var pilot = component.PilotSlot.ContainedEntity;

        // In broken state, equipment, modules, and battery are ejected
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
        {
            _container.Remove(ent, component.EquipmentContainer);
            ScatterEntityFromMech(uid, ent);
        }

        var modules = new List<EntityUid>(component.ModuleContainer.ContainedEntities);
        foreach (var ent in modules)
        {
            _container.Remove(ent, component.ModuleContainer);
            ScatterEntityFromMech(uid, ent);
        }

        if (component.BatterySlot.ContainedEntity != null)
        {
            var battery = component.BatterySlot.ContainedEntity.Value;

            // Remove from container and throw from mech position
            _container.Remove(battery, component.BatterySlot);
            ScatterEntityFromMech(uid, battery);
        }

        // Eject pilot from the mech when entering broken state
        if (pilot.HasValue)
        {
            TryEject(uid, component);
            ScatterEntityFromMech(uid, pilot.Value);
        }

        component.Broken = true;
        UpdateAppearance(uid, component);
        Dirty(uid, component);
        UpdateUserInterface(uid);

        // Play broken sound
        if (component.BrokenSound != null)
        {
            var ev = new MechBrokenSoundEvent(uid, component.BrokenSound);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    /// Repairs a mech that is in broken state, restoring it to normal operation.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="component"></param>
    public void RepairMech(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Broken)
            return;

        // Restore integrity to a safe level above broken threshold
        var repairAmount = component.MaxIntegrity;
        SetIntegrity(uid, repairAmount, component);

        // Reset broken state
        component.Broken = false;

        UpdateAppearance(uid, component);
        Dirty(uid, component);
        UpdateUserInterface(uid);
    }

    /// <summary>
    /// Raised when a mech enters broken state to play sound.
    /// </summary>
    [ByRefEvent]
    public readonly record struct MechBrokenSoundEvent(EntityUid Mech, SoundSpecifier Sound);

    /// <summary>
    /// Raised when a pilot successfully enters a mech and an optional entry sound should be played.
    /// </summary>
    [ByRefEvent]
    public readonly record struct MechEntrySuccessSoundEvent(EntityUid Mech, SoundSpecifier Sound);

    /// <summary>
    /// Checks if an entity can be inserted into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public virtual bool CanInsert(EntityUid uid, EntityUid toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Broken)
            return false;

        if (HasComp<VehicleOperatorComponent>(toInsert))
            return false;

        if (!_actionBlocker.CanMove(toInsert))
            return false;

        if (Vehicle.GetOperatorOrNull(uid) == toInsert)
            return false;

        if (!_container.CanInsert(toInsert, component.PilotSlot))
            return false;

        return true;
    }

    /// <summary>
    /// Updates the user interface
    /// </summary>
    /// <remarks>
    /// This is defined here so that UI updates can be accessed from shared.
    /// </remarks>
    public virtual void UpdateUserInterface(EntityUid uid, MechComponent? component = null)
    {
        RaiseLocalEvent(uid, new UpdateMechUiEvent());
    }

    /// <summary>
    /// Attempts to insert a pilot into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the entity was inserted</returns>
    public bool TryInsert(EntityUid uid, EntityUid toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanInsert(uid, toInsert, component))
            return false;

        _container.Insert(toInsert, component.PilotSlot);
        return true;
    }

    /// <summary>
    /// Attempts to eject the current pilot from the mech
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the pilot was ejected.</returns>
    public bool TryEject(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!Vehicle.TryGetOperator(uid, out var operatorEnt))
            return false;

        _container.RemoveEntity(uid, operatorEnt.Value);
        return true;
    }

    /// <summary>
    /// Grants actions from the mech's action container to the pilot, excluding any actions already attached to the mech itself.
    /// </summary>
    private void GrantMechProvidedActions(EntityUid pilot, EntityUid mech)
    {
        if (!TryComp<ActionsContainerComponent>(mech, out var container))
            return;

        foreach (var actionId in container.Container.ContainedEntities)
        {
            if (_actions.GetAction(actionId) is not { } ent)
                continue;

            // Skip actions already attached to the mech.
            if (ent.Comp.AttachedEntity == mech)
                continue;

            _actions.AddActionDirect(pilot, (ent, ent));
        }
    }

    private void UpdateAppearance(EntityUid uid, MechComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        var isOpen = !Vehicle.HasOperator(uid);

        _appearance.SetData(uid, MechVisuals.Open, isOpen, appearance);
        _appearance.SetData(uid, MechVisuals.Broken, component.Broken, appearance);
    }

    private void OnDragDrop(EntityUid uid, MechComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Dragged, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnCanDragDrop(EntityUid uid, MechComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;

        args.CanDrop |= CanInsert(uid, args.Dragged, component);
    }

    private void OnOperatorSet(Entity<MechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldOperator)
        {
            RemoveUser(ent, oldOperator);
        }

        if (args.NewOperator is { } newOperator)
        {
            SetupUser(ent, newOperator, ent);
            if (ent.Comp.EntrySuccessSound != null)
            {
                var ev = new MechEntrySuccessSoundEvent(ent.Owner, ent.Comp.EntrySuccessSound);
                RaiseLocalEvent(ent, ref ev);
            }
        }

        UpdateAppearance(ent);
    }

    private void OnGetVerb(EntityUid uid, MechComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Enter verb (when user can insert)
        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(enterVerb);
        }
        // Exit verb (when there's an operator)
        else if (Vehicle.HasOperator(uid))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.ExitDelay, new MechExitEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    if (args.User != uid && args.User != component.PilotSlot.ContainedEntity)
                        EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("mech-eject-pilot-alert-popup", ("item", uid), ("user", args.User)), uid);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnEmagged(EntityUid uid, MechComponent component, ref GotEmaggedEvent args)
    {
        if (!component.BreakOnEmag)
            return;
        args.Handled = true;
        component.EquipmentWhitelist = null;
        component.ModuleWhitelist = null;
        Dirty(uid, component);
    }

    private void OnGetMeleeWeapon(EntityUid uid, MechPilotComponent component, GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        var weapon = mech.CurrentSelectedEquipment ?? component.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnGetActiveWeapon(EntityUid uid, MechPilotComponent component, ref GetActiveWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        // Use the currently selected equipment if available, otherwise the mech itself
        var weapon = mech.CurrentSelectedEquipment ?? component.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnGetShootingEntity(EntityUid uid, MechPilotComponent component, ref GetShootingEntityEvent args)
    {
        if (args.Handled)
            return;

        // Use the mech entity for shooting coordinates and physics instead of the pilot
        args.ShootingEntity = component.Mech;
        args.Handled = true;
    }

    private void OnGetProjectileShooter(EntityUid uid, MechPilotComponent component, ref GetProjectileShooterEvent args)
    {
        if (args.Handled)
            return;

        // Use the mech entity as the shooter for projectiles to prevent self-damage
        args.ProjectileShooter = component.Mech;
        args.Handled = true;
    }

    private void OnCanAttackFromContainer(EntityUid uid, MechPilotComponent component, CanAttackFromContainerEvent args)
    {
        args.CanAttack = true;
    }

    private void OnAttackAttempt(EntityUid uid, MechPilotComponent component, AttackAttemptEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancel();
    }

    private void OnRepairAttempt(EntityUid uid, MechComponent component, ref RepairAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (component.Broken)
        {
            args.Cancelled = true;
            return;
        }
    }

    /// <summary>
    /// Returns whether the given mech equipment can be used from hands.
    /// </summary>
    private bool IsMechEquipmentUsableFromHands(Entity<MechEquipmentComponent> ent)
    {
        if (!ent.Comp.BlockUseOutsideMech)
            return true;

        if (ent.Comp.EquipmentOwner.HasValue)
            return true;

        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<MechComponent>(container.Owner))
            return true;

        return false;
    }

    private void OnMechEquipmentShotAttempt(Entity<MechEquipmentComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!IsMechEquipmentUsableFromHands(ent))
            args.Cancel();
    }

    private void OnMechEquipmentMeleeAttempt(Entity<MechEquipmentComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = !IsMechEquipmentUsableFromHands(ent);
    }

    private void OnPilotGetUsedEntity(EntityUid uid, MechPilotComponent pilot, ref GetUsedEntityEvent args)
    {
        // Map pilot interactions to the currently selected equipment on their mech
        if (!TryComp<MechComponent>(pilot.Mech, out var mech))
            return;

        if (!Vehicle.HasOperator(pilot.Mech))
            return;

        if (mech.CurrentSelectedEquipment != null)
            args.Used = mech.CurrentSelectedEquipment;
    }

    private void OnPilotAccessible(EntityUid uid, MechPilotComponent pilot, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;
        args.Accessible = _interaction.IsAccessible(pilot.Mech, args.Target);
    }

    private void OnMechEquipmentGettingUsedAttempt(Entity<MechEquipmentComponent> ent, ref GettingUsedAttemptEvent args)
    {
        // To avoid incorrect empty-hand prediction leading to unintended target activation.
        if (_net.IsClient)
            return;

        if (!ent.Comp.BlockUseOutsideMech)
            return;

        var equipment = ent.Owner;
        var owner = ent.Comp.EquipmentOwner;

        // If the equipment is not inside a mech, block using in hands.
        if (owner == null)
        {
            args.Cancel();
            return;
        }

        // If the equipment is inside a mech, only allow use when it is the currently selected equipment.
        if (!TryComp<MechComponent>(owner.Value, out var mechComp))
        {
            args.Cancel();
            return;
        }

        if (mechComp.CurrentSelectedEquipment != equipment)
            args.Cancel();
    }

    private void OnMechEquipmentUiOpenAttempt(Entity<MechEquipmentComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        // If equipment is outside of a mech, prevent its activatable UI from opening and from adding verbs
        if (ent.Comp.EquipmentOwner == null)
            args.Cancel();
    }

    private void OnUseHeldBypass(EntityUid uid, MechComponent component, ref UseHeldBypassAttemptEvent args)
    {
        // Allow only using mech equipment items on a mech (i.e., inserting).
        if (!_hands.TryGetActiveItem(args.User, out var held))
            return;

        if (HasComp<MechEquipmentComponent>(held.Value))
            args.Bypass = true;
    }
}

/// <summary>
/// Event to request mech UI update (shared between client and server)
/// </summary>
[Serializable, NetSerializable]
public sealed class UpdateMechUiEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveBatteryEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class MechExitEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class MechEntryEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveModuleEvent : SimpleDoAfterEvent
{
}

#region Lock Events
[Serializable, NetSerializable]
public sealed partial class MechDnaLockRegisterEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechDnaLockToggleEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechDnaLockResetEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockRegisterEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockToggleEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockResetEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed partial class MechDnaLockRegisterMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class MechDnaLockToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class MechDnaLockResetMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockRegisterMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class MechCardLockResetMessage : BoundUserInterfaceMessage
{
}
#endregion
