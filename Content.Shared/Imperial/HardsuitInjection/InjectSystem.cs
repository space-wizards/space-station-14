using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
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
using Robust.Shared.Utility;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class InjectSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _sharedAdminLogSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InjectComponent, ToggleInjectionEvent>(OnToggleClothing);
        SubscribeLocalEvent<InjectComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<InjectComponent, ComponentRemove>(OnRemoveToggleable);
        SubscribeLocalEvent<InjectComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<InjectComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<InjectComponent, ToggleSlotDoAfterEvent>(OnDoAfterComplete);
        SubscribeLocalEvent<InjectComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<InjectComponent, InjectionEvent>(InjectOther);
        SubscribeLocalEvent<AmpulaComponent, EntGotInsertedIntoContainerMessage>(Inserted);
        SubscribeLocalEvent<InjectComponent, UpdateEvent>(UpdateThing);
        SubscribeLocalEvent<InjectNeedComponent, MobStateChangedEvent>(Changed);
        SubscribeLocalEvent<InjectComponent, EjectionEvent>(Ejection);
    }
    private void InjectOther(EntityUid uid, InjectComponent component, InjectionEvent args)
    {
        args.Performer = uid;
        Inject(uid, component, args);
    }
    private void Ejection(EntityUid uid, InjectComponent component, EjectionEvent args)
    {
        if (args.Handled)
            return;
        if (_netManager.IsClient)
            return;
        var sys = _entManager.System<ItemSlotsSystem>();
        if (!TryComp<ItemSlotsComponent>(component.Owner, out var itemslots))
            return;
        if (!sys.TryGetSlot(component.Owner, component.ContainerId, out var slot, itemslots))
            return;
        if (slot.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-closed"), args.Performer, args.Performer);
            return;
        }
        if (slot.ContainerSlot == null || slot.ContainerSlot.ContainedEntity == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-nobeaker"), args.Performer, args.Performer);
            return;
        }
        sys.TryEjectToHands(component.Owner, slot, args.Performer);
    }
    private void Changed(EntityUid uid, InjectNeedComponent injectneed, MobStateChangedEvent args)
    {
        if (_netManager.IsClient)
            return;
        if (args.NewMobState == MobState.Invalid || args.NewMobState == MobState.Alive)
            return;
        if (!TryComp<InventoryComponent>(args.Target, out var inventory))
            return;
        if (!_entManager.System<InventorySystem>().TryGetSlotEntity(args.Target, "outerClothing", out var slot, inventory))
            return;
        if (!TryComp<ItemSlotsComponent>(slot, out var itemslots))
            return;
        if (!TryComp<InjectComponent>(slot, out var component))
            return;
        _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-critical"), component.Owner, PopupType.Medium);
        Inject(component.Owner, component, new InjectionEvent() { Performer = uid });

    }
    private void Inserted(EntityUid uid, AmpulaComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (!TryComp<InjectComponent>(args.Container.Owner, out var inject))
            return;
        if (!_actionsSystem.TryGetActionData(inject.ActionEntity, out var action) ||
            action.AttachedEntity == null)
            return;
        var towho = action.AttachedEntity;
        if (!TryComp<MobStateComponent>(towho, out var state))
            return;
        if (state.CurrentState == MobState.Invalid || state.CurrentState == MobState.Alive)
            return;
        _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-critical"), args.Container.Owner, PopupType.Medium);
        Inject(args.Container.Owner, inject, new InjectionEvent() { Performer = uid });
    }
    private void UpdateThing(EntityUid uid, InjectComponent component, UpdateEvent args)
    {
        if (!args.Uid.HasValue || !args.Realtransfer.HasValue || args.Solution == null)
            return;
        var ui = GetEntity(args.Uid.Value);
        if (!TryComp<AppearanceComponent>(ui, out var appearance))
            return;
        var removedSolution = _solutions.SplitSolution(ui, args.Solution, args.Realtransfer.Value);
        args.End = removedSolution;
        _entManager.System<SolutionContainerSystem>().UpdateAppearance(ui, args.Solution, appearance);
    }
    private void OnExamine(EntityUid uid, InjectComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("hardsuitinjection-" + component.Locked.ToString()));
    }
    private void GetRelayedVerbs(EntityUid uid, InjectComponent component, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbs(uid, component, args.Args);
    }

    private void OnGetVerbs(EntityUid uid, InjectComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Container == null)
            return;

        var text = component.VerbText ?? (component.ActionEntity == null ? null : Name(component.ActionEntity.Value));
        var text2 = "hardsuitinjection-eject";
        if (text == null || text2 == null)
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
            verb.ExecutionEventArgs = new ToggleInjectionEvent() { Performer = args.User };
        }
        else
        {
            verb.Act = () => StartDoAfter(args.User, uid, Transform(uid).ParentUid, component);
        }
        var verb2 = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Text = Loc.GetString(text2)
        };

        verb2.EventTarget = uid;
        verb2.ExecutionEventArgs = new EjectionEvent() { Performer = args.User };

        args.Verbs.Add(verb);
        args.Verbs.Add(verb2);
    }

    private void StartDoAfter(EntityUid user, EntityUid item, EntityUid wearer, InjectComponent component)
    {
        if (component.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, (float) component.StripDelay.Value.TotalSeconds);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleSlotDoAfterEvent(), item, wearer, item)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;
        if (component.Locked)
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} is trying to open ES of {_entManager.ToPrettyString(wearer):wearer}");
        else
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} is trying to close ES of {_entManager.ToPrettyString(wearer):wearer}");
        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", item));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnDoAfterComplete(EntityUid uid, InjectComponent component, ToggleSlotDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;
        if (_netManager.IsClient)
            return;
        ToggleClothing(args.User, component, uid);
        args.Handled = true;
    }

    private void OnRemoveToggleable(EntityUid uid, InjectComponent component, ComponentRemove args)
    {
        if (_actionsSystem.TryGetActionData(component.ActionEntity, out var action) &&
            action.AttachedEntity != null)
        {
            _actionsSystem.RemoveAction(action.AttachedEntity.Value, component.ActionEntity);
        }
        if (_actionsSystem.TryGetActionData(component.InjectActionEntity, out var injectaction) &&
            injectaction.AttachedEntity != null)
        {
            _actionsSystem.RemoveAction(injectaction.AttachedEntity.Value, component.InjectActionEntity);
        }
    }
    private void OnToggleClothing(EntityUid uid, InjectComponent component, ToggleInjectionEvent args)
    {
        if (args.Handled)
            return;
        if (_netManager.IsClient)
            return;
        args.Handled = true;
        ToggleClothing(args.Performer, component, uid);
    }

    private void Inject(EntityUid uid, InjectComponent component, InjectionEvent args)
    {
        if (args.Handled)
            return;
        if (_netManager.IsClient)
            return;
        if (_actionsSystem.TryGetActionData(component.InjectActionEntity, out var action))
        {
            if (action.AttachedEntity == null)
                return;
            if (TryComp<ItemSlotsComponent>(action.AttachedEntity, out var itemslots))
                return;
            var sys = _entManager.System<ItemSlotsSystem>();
            var user = action.AttachedEntity.Value;
            var beaker = sys.GetItemOrNull(uid, component.ContainerId, itemslots);
            if (beaker == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-nobeaker"), user, user);
                return;
            }
            var actualbeaker = beaker.Value;
            if (!_solutions.TryGetSolution(actualbeaker, "beaker", out var solution))
                return;
            if (!_solutions.TryGetInjectableSolution(user, out var targetSolution))
                return;
            if (solution.Volume <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-empty"), user, user);
                return;
            }
            var realTransferAmount = FixedPoint2.Min(solution.Volume, targetSolution.AvailableVolume);
            if (realTransferAmount <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-full"), user, user);
                return;
            }
            var ev = new UpdateEvent(GetNetEntity(actualbeaker), solution, realTransferAmount);
            RaiseLocalEvent(uid, ev);
            if (ev.End == null)
                return;
            var removedSolution = ev.End;
            if (!targetSolution.CanAddSolution(removedSolution))
                return;
            if (args.Performer == uid)
                _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} injected his ES into yourself with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");
            else
                _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} ES injected with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");
            _reactiveSystem.DoEntityReaction(user, removedSolution, ReactionMethod.Injection);
            _solutions.TryAddSolution(user, targetSolution, removedSolution);
            _audio.PlayPvs(component.InjectSound, user);
            _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), user, user);

            args.Handled = true;
        }
    }


    private void ToggleClothing(EntityUid user, InjectComponent component, EntityUid uid)
    {
        if (component.Container == null)
            return;
        if (!TryComp<ItemSlotsComponent>(uid, out var comp))
            return;
        component.Locked = !component.Locked;
        if (component.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-close"), user, user, PopupType.Medium);
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} closed ES of {_entManager.ToPrettyString(uid):wearer}");
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-open"), user, user, PopupType.Medium);
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} opened ES of {_entManager.ToPrettyString(uid):wearer}");
        }
        _entManager.System<ItemSlotsSystem>().SetLock(uid, component.ContainerId, component.Locked, comp);
    }

    private void OnGetActions(EntityUid uid, InjectComponent component, GetItemActionsEvent args)
    {
        if ((args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(ref component.ActionEntity, component.Action);
        }
        if ((args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(ref component.InjectActionEntity, component.InjectAction);
        }
    }

    private void OnInit(EntityUid uid, InjectComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<ContainerSlot>(uid, component.ContainerId);
        if (!TryComp<ItemSlotsComponent>(uid, out var comp))
            return;
        _entManager.System<ItemSlotsSystem>().SetLock(uid, component.ContainerId, component.Locked, comp);
    }
}
public sealed partial class ToggleInjectionEvent : InstantActionEvent
{
}
public sealed partial class InjectionEvent : InstantActionEvent
{
}
public sealed partial class EjectionEvent : InstantActionEvent
{
}


[Serializable, NetSerializable]
public sealed partial class ToggleSlotDoAfterEvent : SimpleDoAfterEvent
{
}
[Serializable, NetSerializable]
public sealed class UpdateEvent : EntityEventArgs
{
    public NetEntity? Uid = null;
    public Solution? Solution = null;
    public FixedPoint2? Realtransfer = null;
    public Solution? End = null;
    public UpdateEvent(NetEntity uid, Solution? solution, FixedPoint2 realtransfer)
    {
        Uid = uid;
        Solution = solution;
        Realtransfer = realtransfer;
    }
}
