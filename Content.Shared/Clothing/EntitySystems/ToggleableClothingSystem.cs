using System.Collections.Immutable;
using System.Linq;
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
        GetVerbs(uid, null, component, args.Args);
    }

    private void OnGetVerbs(EntityUid uid, ToggleableClothingComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        GetVerbs(uid, null, component, args);
    }

    private void GetVerbs(EntityUid uid,
        EntityUid? targetClothing,
        ToggleableClothingComponent component,
        GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || component.Container == null)
            return;

        if (!_inventorySystem.InSlotWithFlags(uid, component.RequiredFlags))
            return;

        var wearer = Transform(uid).ParentUid;
        if (args.User != wearer && component.StripDelay == null)
            return;

        // Handle the case of only a single kind specially so there's no needless nesting.
        if (component.ClothingUids.Count == 1 || targetClothing != null)
        {
            var target = targetClothing ?? component.ClothingUids.First().Key;

            var verb = new EquipmentVerb()
            {
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Text = Loc.GetString(component.VerbText, ("entity", target)),
            };

            if (args.User == wearer)
            {
                verb.EventTarget = uid;
                verb.ExecutionEventArgs = new ToggleClothingEvent(target: target) { Performer = args.User };
            }
            else
            {
                verb.Act = () => StartDoAfter(args.User, uid, target, Transform(uid).ParentUid, component);
            }

            args.Verbs.Add(verb);
        }
        else
        {
            foreach (var (target, slot) in component.ClothingUids)
            {
                var verb = new EquipmentVerb()
                {
                    Text = Loc.GetString(component.VerbText, ("entity", target)),
                    IconEntity = GetNetEntity(target),
                    Category = VerbCategory.ToggleClothing,
                };

                if (args.User == wearer)
                {
                    verb.EventTarget = uid;
                    verb.ExecutionEventArgs = new ToggleClothingEvent(target: target) { Performer = args.User };
                }
                else
                {
                    verb.Act = () => StartDoAfter(args.User, uid, target, Transform(uid).ParentUid, component);
                }

                args.Verbs.Add(verb);
            }
        }
    }

    private void StartDoAfter(EntityUid user, EntityUid item, EntityUid targetClothing, EntityUid wearer, ToggleableClothingComponent component)
    {
        if (component.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, item, component.StripDelay.Value);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleClothingDoAfterEvent(GetNetEntity(targetClothing)), item, wearer, item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
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
        // Use the parents GetVerbs but specify that this child is the target.
        GetVerbs(component.AttachedUid, uid, Comp<ToggleableClothingComponent>(component.AttachedUid), args);
    }

    private void OnDoAfterComplete(EntityUid uid, ToggleableClothingComponent component, ToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        DoMultiToggle(args.User, uid, GetEntity(args.TargetClothing), component);
    }

    private void OnInteractHand(EntityUid uid, AttachedClothingComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(component.AttachedUid, out ToggleableClothingComponent? toggleCom)
            || toggleCom.Container == null)
            return;

        if (!toggleCom.ClothingUids.TryGetValue(uid, out var slot))
            return;

        if (!_inventorySystem.TryUnequip(Transform(uid).ParentUid, slot, force: true))
            return;

        _containerSystem.Insert(uid, toggleCom.Container);
        args.Handled = true;
    }

    /// <summary>
    ///     Called when the clothing is unequipped, to ensure that all attached clothing also gets unequipped.
    /// </summary>
    private void OnToggleableUnequip(EntityUid uid, ToggleableClothingComponent component, GotUnequippedEvent args)
    {
        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        if (component.Container == null)
            return;

        foreach (var (entity, slot) in component.ClothingUids)
        {
            // Assume that if a particular entity is not in the internal container then it is currently equipped.
            // Log if that is not the case (though this system has been working correctly this entire time).
            if (component.Container.Contains(entity))
                continue;

            if (!_inventorySystem.TryUnequip(args.EquipTarget,
                    slot,
                    out var removed,
                    force: true,
                    triggerHandContact: true))
                continue;

            if (removed != entity)
                Log.Warning($"Unequipped {ToPrettyString(removed)} instead of {ToPrettyString(entity)} when removing Toggleable {ToPrettyString(uid)}");
        }
    }

    private void OnRemoveToggleable(EntityUid uid, ToggleableClothingComponent component, ComponentRemove args)
    {
        // If the parent/owner component of the attached clothing is being removed (entity getting deleted?) we will
        // delete the attached entity. We do this regardless of whether or not the attached entity is currently
        // "outside" of the container or not. This means that if a hardsuit takes too much damage, the helmet will also
        // automatically be deleted.

        _actionsSystem.RemoveAction(component.ActionEntity);

        if (!_netMan.IsClient)
        {
            foreach (var entity in component.ClothingUids.Keys)
                QueueDel(entity);
        }
    }

    private void OnAttachedUnequipAttempt(EntityUid uid, AttachedClothingComponent component, BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(EntityUid uid, AttachedClothingComponent component, ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will remove it from the set
        // of entities that the toggleable clothing can toggle. If there are no entities left then we just remove
        // the entire toggleable component. There is currently no way to fix a piece of clothing that has lost
        // one of its attached pieces.

        if (!TryComp(component.AttachedUid, out ToggleableClothingComponent? toggleComp))
            return;

        if (toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        toggleComp.ClothingUids.Remove(uid);

        // There are still other AttachedClothing pieces left that could be used, so keep the component.
        if (toggleComp.ClothingUids.Count != 0)
            return;

        _actionsSystem.RemoveAction(toggleComp.ActionEntity);
        RemComp(component.AttachedUid, toggleComp);
    }

    /// <summary>
    ///     Called if the clothing was unequipped, to ensure that it gets moved into the parent's container.
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

        if (LifeStage(component.AttachedUid) > EntityLifeStage.MapInitialized)
            return;

        // As unequipped gets called in the middle of container removal, we cannot call a container-insert without causing issues.
        // So we delay it and process it during a system update:
        if (toggleComp.Container != null)
            _containerSystem.Insert(uid, toggleComp.Container);
    }

    /// <summary>
    ///     Equip or unequip the toggleable clothing.
    /// </summary>
    private void OnToggleClothing(EntityUid uid, ToggleableClothingComponent component, ToggleClothingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        DoMultiToggle(args.Performer, uid, args.TargetClothing, component);
    }

    private void DoMultiToggle(EntityUid user,
        EntityUid target,
        EntityUid? targetClothing,
        ToggleableClothingComponent component)
    {
        // We want to toggle the target if we have one.
        // Otherwise, if there is some clothing that isn't deployed, then we deploy all undeployed clothing.
        // If there is no undeployed clothing, then we retract everything instead.
        // This preserves the default Action behavior on single piece ToggleableClothing, and provides a sensible
        // default on multi-piece ToggleableClothing. Though multi-piece could also provide a different action
        // that provides a method of selecting a piece instead.
        if (targetClothing != null)
        {
            ToggleClothing(user, target, targetClothing.Value, component);
            return;
        }

        if (component.Container is {} container && container.ContainedEntities.Count > 0)
        {
            // Determine if any of the unequipped clothes can be equipped. If they can, put them on. If they can't, then
            // instead try to unequip any currently equipped ones. This is so that if the user is wearing a piece of
            // clothing that blocks only one of the slots, the default action continues to function for the rest of the
            // slots.
            var unequippedClothing = container.ContainedEntities.Where(clothing =>
                    CanEquip(user, clothing, component.ClothingUids[clothing]))
                .ToArray();

            if (unequippedClothing.Length != 0)
            {
                foreach (var entity in unequippedClothing)
                {
                    ToggleClothing(user, target, entity, component);
                }

                return;
            }
        }

        foreach (var entity in component.ClothingUids.Keys)
        {
            if (!component.Container?.Contains(entity) ?? false)
                ToggleClothing(user, target, entity, component);
        }
    }

    private bool CanEquip(EntityUid user, EntityUid targetClothing, string slot)
    {
        if (_inventorySystem.TryGetSlotEntity(user, slot, out _))
            return false;

        return _inventorySystem.CanEquip(user, targetClothing, slot, out _);
    }

    private void ToggleClothing(EntityUid user, EntityUid target, EntityUid targetClothing, ToggleableClothingComponent component)
    {
        if (component.Container == null || !component.ClothingUids.ContainsKey(targetClothing))
            return;

        var parent = Transform(target).ParentUid;
        var slot = component.ClothingUids[targetClothing];
        if (!component.Container.Contains(targetClothing))
        {
            _inventorySystem.TryUnequip(user, parent, slot, force: true);
        }
        else if (_inventorySystem.TryGetSlotEntity(parent, slot, out var existing))
        {
            _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                user, user);
        }
        else
            _inventorySystem.TryEquip(user, parent, targetClothing, slot, triggerHandContact: true);
    }

    private void OnGetActions(EntityUid uid, ToggleableClothingComponent component, GetItemActionsEvent args)
    {
        if (component.ActionEntity != null
            && (args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(component.ActionEntity.Value);
        }
    }

    private void OnInit(EntityUid uid, ToggleableClothingComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<Container>(uid, component.ContainerId);
    }

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the toggled-entity.
    /// </summary>
    private void OnMapInit(EntityUid uid, ToggleableClothingComponent component, MapInitEvent args)
    {
        if (component.Container!.ContainedEntities is {} ents)
        {
            foreach (var ent in ents)
            {
                DebugTools.Assert(component.ClothingUids.ContainsKey(ent), "Unexpected entity present inside of a toggleable clothing container.");
                return;
            }
        }

        if (component.ClothingUids.Count == 0 && component.ActionEntity != null)
        {
            foreach (var entity in component.ClothingUids.Keys)
            {
                DebugTools.Assert(Exists(entity), "Toggleable clothing is missing expected entity.");
                DebugTools.Assert(TryComp(entity, out AttachedClothingComponent? comp), "Toggleable clothing is missing an attached component");
                DebugTools.Assert(comp?.AttachedUid == uid, "Toggleable clothing uid mismatch");
            }
        }
        else
        {
            var xform = Transform(uid);
            foreach (var (slot, protoIds) in component.ClothingPrototypes)
            {
                foreach (var protoId in protoIds)
                {
                    var entity = Spawn(protoId, xform.Coordinates);
                    component.ClothingUids.Add(entity, slot);
                    var attachedClothing = EnsureComp<AttachedClothingComponent>(entity);
                    attachedClothing.AttachedUid = uid;
                    Dirty(entity, attachedClothing);
                    _containerSystem.Insert(entity, component.Container, containerXform: xform);
                }
            }
            Dirty(uid, component);
        }

        if (_actionContainer.EnsureAction(uid, ref component.ActionEntity, out var action, component.Action))
        {
            _actionsSystem.SetEntityIcon((component.ActionEntity.Value, action), component.ClothingUids.First().Key);
        }
    }
}

public sealed partial class ToggleClothingEvent : InstantActionEvent
{
    public EntityUid? TargetClothing;

    public ToggleClothingEvent(EntityUid target)
    {
        TargetClothing = target;
    }
}

[Serializable, NetSerializable]
public sealed partial class ToggleClothingDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetClothing;

    public ToggleClothingDoAfterEvent(NetEntity target)
    {
        TargetClothing = target;
    }
}
