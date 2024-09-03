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

    private void GetRelayedVerbs(Entity<ToggleableClothingComponent> toggleable, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbs(toggleable, ref args.Args);
    }

    private void OnGetVerbs(Entity<ToggleableClothingComponent> toggleable, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = toggleable.Comp;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || comp.ClothingUids.Count == 0 || comp.Container == null)
            return;

        var text = comp.VerbText ?? (comp.ActionEntity == null ? null : Name(comp.ActionEntity.Value));
        if (text == null)
            return;

        if (!_inventorySystem.InSlotWithFlags(toggleable.Owner, comp.RequiredFlags))
            return;

        var wearer = Transform(toggleable).ParentUid;
        if (args.User != wearer && comp.StripDelay == null)
            return;

        var user = args.User;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString(text),
        };

        if (user == wearer)
        {
            verb.EventTarget = toggleable;
            verb.ExecutionEventArgs = new ToggleClothingEvent() { Performer = args.User };
        }
        else
        {
            verb.Act = () => StartDoAfter(user, toggleable, wearer);
        }

        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid user, Entity<ToggleableClothingComponent> toggleable, EntityUid wearer)
    {
        var comp = toggleable.Comp;

        if (comp.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, toggleable, comp.StripDelay.Value);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleClothingDoAfterEvent(), toggleable, wearer, toggleable)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", toggleable));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnGetAttachedStripVerbsEvent(Entity<AttachedClothingComponent> attached, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = attached.Comp;

        if (!TryComp<ToggleableClothingComponent>(comp.AttachedUid, out var toggleableComp))
            return;

        // redirect to the attached entity.
        OnGetVerbs((comp.AttachedUid, toggleableComp), ref args);
    }

    private void OnDoAfterComplete(Entity<ToggleableClothingComponent> toggleable, ref ToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ToggleClothing(args.User, toggleable);
    }

    private void OnInteractHand(Entity<AttachedClothingComponent> attached, ref InteractHandEvent args)
    {
        var comp = attached.Comp;

        if (args.Handled)
            return;

        if (!TryComp(comp.AttachedUid, out ToggleableClothingComponent? toggleableComp)
            || toggleableComp.Container == null)
            return;

        // Get slot from dictionary of uid-slot
        if (!toggleableComp.ClothingUids.TryGetValue(attached.Owner, out var attachedSlot))
            return;

        if (!_inventorySystem.TryUnequip(Transform(attached.Owner).ParentUid, attachedSlot, force: true))
            return;

        _containerSystem.Insert(attached.Owner, toggleableComp.Container);
        args.Handled = true;
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that the helmet also gets unequipped.
    /// </summary>
    private void OnToggleableUnequip(Entity<ToggleableClothingComponent> toggleable, ref GotUnequippedEvent args)
    {
        var comp = toggleable.Comp;

        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        // Check if container exists and we have linked clothings
        if (comp.Container == null || comp.ClothingUids.Count == 0)
            return;

        var parts = comp.ClothingUids;

        foreach (var part in parts)
        {
            // Check if entity in container what means it already unequipped
            if (comp.Container.Contains(part.Key))
                continue;

            if (part.Value == null)
                continue;

            _inventorySystem.TryUnequip(args.Equipee, part.Value, force: true);
        }
    }

    private void OnRemoveToggleable(Entity<ToggleableClothingComponent> toggleable, ref ComponentRemove args)
    {
        // If the parent/owner component of the attached clothing is being removed (entity getting deleted?) we will
        // delete the attached entity. We do this regardless of whether or not the attached entity is currently
        // "outside" of the container or not. This means that if a hardsuit takes too much damage, the helmet will also
        // automatically be deleted.

        var comp = toggleable.Comp;

        _actionsSystem.RemoveAction(comp.ActionEntity);

        if (comp.ClothingUids == null || _netMan.IsClient)
            return;

        foreach (var clothing in comp.ClothingUids.Keys)
        {
            QueueDel(clothing);
        }
    }

    private void OnAttachedUnequipAttempt(Entity<AttachedClothingComponent> attached, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(Entity<AttachedClothingComponent> attached, ref ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will just remove the
        // toggleable clothing component. This means if you had a hard-suit helmet that took too much damage, you would
        // still be left with a suit that was simply missing a helmet. There is currently no way to fix a partially
        // broken suit like this.

        var comp = attached.Comp;

        if (!TryComp(comp.AttachedUid, out ToggleableClothingComponent? toggleableComp))
            return;

        if (toggleableComp.LifeStage > ComponentLifeStage.Running)
            return;

        var clothingUids = toggleableComp.ClothingUids;

        if (!clothingUids.Remove(attached.Owner))
            return;

        // If no attached clothing left - remove component and action
        if (clothingUids.Count > 0)
            return;

        _actionsSystem.RemoveAction(toggleableComp.ActionEntity);
        RemComp(comp.AttachedUid, toggleableComp);
    }

    /// <summary>
    ///     Called if the clothing was unequipped, to ensure that it gets moved into the suit's container.
    /// </summary>
    private void OnAttachedUnequip(Entity<AttachedClothingComponent> attached, ref GotUnequippedEvent args)
    {
        var comp = attached.Comp;

        // Let containers worry about it.
        if (_timing.ApplyingState)
            return;

        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(comp.AttachedUid, out ToggleableClothingComponent? toggleableComp))
            return;

        if (toggleableComp.LifeStage > ComponentLifeStage.Running)
            return;

        // As unequipped gets called in the middle of container removal, we cannot call a container-insert without causing issues.
        // So we delay it and process it during a system update:
        if (!toggleableComp.ClothingUids.ContainsKey(attached.Owner))
            return;

        if (toggleableComp.Container != null)
            _containerSystem.Insert(attached, toggleComp.Container);
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
            _containerSystem.Insert(component.ClothingUid.Value, component.Container, containerXform: xform);
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
