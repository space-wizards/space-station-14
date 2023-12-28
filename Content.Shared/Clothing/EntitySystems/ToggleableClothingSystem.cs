using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ToggleableClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableClothingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ToggleableClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleableClothingComponent, ToggleClothingEvent>(OnToggleClothing);
        SubscribeLocalEvent<ToggleableClothingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ToggleableClothingComponent, ComponentRemove>(OnRemoveToggleable);
        SubscribeLocalEvent<ToggleableClothingComponent, GotUnequippedEvent>(OnToggleableUnequip);

        SubscribeLocalEvent<AttachedClothingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AttachedClothingComponent, GotUnequippedEvent>(OnAttachedUnequip);
        SubscribeLocalEvent<AttachedClothingComponent, ComponentRemove>(OnRemoveAttached);
        SubscribeLocalEvent<AttachedClothingComponent, BeingUnequippedAttemptEvent>(OnAttachedUnequipAttempt);

        SubscribeLocalEvent<ToggleableClothingComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<ToggleableClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AttachedClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetAttachedStripVerbsEvent);
        SubscribeLocalEvent<ToggleableClothingComponent, ToggleClothingDoAfterEvent>(OnDoAfterComplete);
    }

    private void GetRelayedVerbs(EntityUid uid, ToggleableClothingComponent component, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbs(uid, component, args.Args);
    }

    private void OnGetVerbs(EntityUid uid, ToggleableClothingComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.ClothingUid == null || component.Container == null)
            return;

        var text = component.VerbText ?? (component.ActionEntity == null ? null : Name(component.ActionEntity.Value));
        if (text == null)
            return;

        if (!_inventorySystem.InSlotWithFlags(uid, component.RequiredFlags))
            return;

        var wearer = Transform(uid).ParentUid;
        if (args.User != wearer && component.StripDelay == null)
            return;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString(text),
        };

        if (args.User == wearer)
        {
            verb.EventTarget = uid;
            verb.ExecutionEventArgs = new ToggleClothingEvent() { Performer = args.User };
        }
        else
        {
            verb.Act = () => StartDoAfter(args.User, uid, Transform(uid).ParentUid, component);
        }

        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid user, EntityUid item, EntityUid wearer, ToggleableClothingComponent component)
    {
        if (component.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, (float) component.StripDelay.Value.TotalSeconds);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleClothingDoAfterEvent(), item, wearer, item)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            // This should just re-use the BUI range checks & cancel the do after if the BUI closes. But that is all
            // server-side at the moment.
            // TODO BUI REFACTOR.
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", item));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnGetAttachedStripVerbsEvent(EntityUid uid, AttachedClothingComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        // redirect to the attached entity.
        OnGetVerbs(component.AttachedUid, Comp<ToggleableClothingComponent>(component.AttachedUid), args);
    }

    private void OnDoAfterComplete(EntityUid uid, ToggleableClothingComponent component, ToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ToggleClothing(args.User, uid, component);
    }

    private void OnInteractHand(EntityUid uid, AttachedClothingComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(component.AttachedUid, out ToggleableClothingComponent? toggleCom)
            || toggleCom.Container == null)
            return;

        if (!_inventorySystem.TryUnequip(Transform(uid).ParentUid, toggleCom.Slot, force: true))
            return;

        toggleCom.Container.Insert(uid, EntityManager);
        args.Handled = true;
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that the helmet also gets unequipped.
    /// </summary>
    private void OnToggleableUnequip(EntityUid uid, ToggleableClothingComponent component, GotUnequippedEvent args)
    {
        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        // If the attached clothing is not currently in the container, this just assumes that it is currently equipped.
        // This should maybe double check that the entity currently in the slot is actually the attached clothing, but
        // if its not, then something else has gone wrong already...
        if (component.Container != null && component.Container.ContainedEntity == null && component.ClothingUid != null)
            _inventorySystem.TryUnequip(args.Equipee, component.Slot, force: true);
    }

    private void OnRemoveToggleable(EntityUid uid, ToggleableClothingComponent component, ComponentRemove args)
    {
        // If the parent/owner component of the attached clothing is being removed (entity getting deleted?) we will
        // delete the attached entity. We do this regardless of whether or not the attached entity is currently
        // "outside" of the container or not. This means that if a hardsuit takes too much damage, the helmet will also
        // automatically be deleted.

        // remove action.
        if (_actionsSystem.TryGetActionData(component.ActionEntity, out var action) &&
            action.AttachedEntity != null)
        {
            _actionsSystem.RemoveAction(action.AttachedEntity.Value, component.ActionEntity);
        }

        if (component.ClothingUid != null && !_netMan.IsClient)
            QueueDel(component.ClothingUid.Value);
    }

    private void OnAttachedUnequipAttempt(EntityUid uid, AttachedClothingComponent component, BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(EntityUid uid, AttachedClothingComponent component, ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will just remove the
        // toggleable clothing component. This means if you had a hard-suit helmet that took too much damage, you would
        // still be left with a suit that was simply missing a helmet. There is currently no way to fix a partially
        // broken suit like this.

        if (!TryComp(component.AttachedUid, out ToggleableClothingComponent? toggleComp))
            return;

        if (toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        // remove action.
        if (_actionsSystem.TryGetActionData(toggleComp.ActionEntity, out var action) &&
            action.AttachedEntity != null)
        {
            _actionsSystem.RemoveAction(action.AttachedEntity.Value, toggleComp.ActionEntity);
        }

        RemComp(component.AttachedUid, toggleComp);
    }

    /// <summary>
    ///     Called if the helmet was unequipped, to ensure that it gets moved into the suit's container.
    /// </summary>
    private void OnAttachedUnequip(EntityUid uid, AttachedClothingComponent component, GotUnequippedEvent args)
    {
        // Let containers worry about it.
        if (_timing.ApplyingState)
            return;

        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(component.AttachedUid, out ToggleableClothingComponent? toggleComp))
            return;

        if (toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        // As unequipped gets called in the middle of container removal, we cannot call a container-insert without causing issues.
        // So we delay it and process it during a system update:
        if (toggleComp.ClothingUid != null)
            toggleComp.Container?.Insert(toggleComp.ClothingUid.Value);
    }

    /// <summary>
    ///     Equip or unequip the toggleable clothing.
    /// </summary>
    private void OnToggleClothing(EntityUid uid, ToggleableClothingComponent component, ToggleClothingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleClothing(args.Performer, uid, component);
    }

    private void ToggleClothing(EntityUid user, EntityUid target, ToggleableClothingComponent component)
    {
        if (component.Container == null || component.ClothingUid == null)
            return;

        var parent = Transform(target).ParentUid;
        if (component.Container.ContainedEntity == null)
            _inventorySystem.TryUnequip(user, parent, component.Slot, force: true);
        else if (_inventorySystem.TryGetSlotEntity(parent, component.Slot, out var existing))
        {
            _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                user, user);
        }
        else
            _inventorySystem.TryEquip(user, parent, component.ClothingUid.Value, component.Slot);
    }

    private void OnGetActions(EntityUid uid, ToggleableClothingComponent component, GetItemActionsEvent args)
    {
        if (component.ClothingUid != null
            && component.ActionEntity != null
            && (args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(component.ActionEntity.Value);
        }
    }

    private void OnInit(EntityUid uid, ToggleableClothingComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<ContainerSlot>(uid, component.ContainerId);
    }

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the toggled-entity.
    /// </summary>
    private void OnMapInit(EntityUid uid, ToggleableClothingComponent component, MapInitEvent args)
    {
        if (component.Container!.ContainedEntity is {} ent)
        {
            DebugTools.Assert(component.ClothingUid == ent, "Unexpected entity present inside of a toggleable clothing container.");
            return;
        }

        if (component.ClothingUid != null && component.ActionEntity != null)
        {
            DebugTools.Assert(Exists(component.ClothingUid), "Toggleable clothing is missing expected entity.");
            DebugTools.Assert(TryComp(component.ClothingUid, out AttachedClothingComponent? comp), "Toggleable clothing is missing an attached component");
            DebugTools.Assert(comp?.AttachedUid == uid, "Toggleable clothing uid mismatch");
        }
        else
        {
            var xform = Transform(uid);
            component.ClothingUid = Spawn(component.ClothingPrototype, xform.Coordinates);
            var attachedClothing = EnsureComp<AttachedClothingComponent>(component.ClothingUid.Value);
            attachedClothing.AttachedUid = uid;
            Dirty(component.ClothingUid.Value, attachedClothing);
            component.Container.Insert(component.ClothingUid.Value, EntityManager, ownerTransform: xform);
            Dirty(uid, component);
        }

        if (_actionContainer.EnsureAction(uid, ref component.ActionEntity, out var action, component.Action))
            _actionsSystem.SetEntityIcon(component.ActionEntity.Value, component.ClothingUid, action);
    }
}

public sealed partial class ToggleClothingEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ToggleClothingDoAfterEvent : SimpleDoAfterEvent
{
}
