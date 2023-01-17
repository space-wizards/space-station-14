using System.Linq;
using System.Threading;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Mech.Components;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Server.Wires;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("mech");

        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, MechEntryFinishedEvent>(OnEntryFinished);
        SubscribeLocalEvent<MechComponent, MechEntryCanclledEvent>(OnEntryExitCancelled);
        SubscribeLocalEvent<MechComponent, MechExitFinishedEvent>(OnExitFinished);
        SubscribeLocalEvent<MechComponent, MechExitCanclledEvent>(OnEntryExitCancelled);
        SubscribeLocalEvent<MechComponent, MechRemoveBatteryFinishedEvent>(OnRemoveBatteryFinished);
        SubscribeLocalEvent<MechComponent, MechRemoveBatteryCancelledEvent>(OnRemoveBatteryCancelled);

        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);

        SubscribeLocalEvent<MechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);


        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);

        #region Equipment UI message relays
        SubscribeLocalEvent<MechComponent, MechGrabberEjectMessage>(RecieveEquipmentUiMesssages);
        #endregion
    }

    private void OnMechCanMoveEvent(EntityUid uid, MechComponent component , UpdateCanMoveEvent args)
    {
        if (component.Broken || component.Integrity <= 0 || component.Energy <= 0)
            args.Cancel();
    }
    private void OnInteractUsing(EntityUid uid, MechComponent component, InteractUsingEvent args)
    {
        if (TryComp<WiresComponent>(uid, out var wires) && !wires.IsPanelOpen)
            return;

        if (component.BatterySlot.ContainedEntity == null && TryComp<BatteryComponent>(args.Used, out var battery))
        {
            InsertBattery(uid, args.Used, component, battery);
            _actionBlocker.UpdateCanMove(uid);
            return;
        }

        if (component.EntryTokenSource == null &&
            TryComp<ToolComponent>(args.Used, out var tool) &&
            tool.Qualities.Contains("Prying") &&
            component.BatterySlot.ContainedEntity != null)
        {
            component.EntryTokenSource = new();
            _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.BatteryRemovalDelay, component.EntryTokenSource.Token, uid, args.Target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                TargetFinishedEvent = new MechRemoveBatteryFinishedEvent(),
                TargetCancelledEvent = new MechRemoveBatteryCancelledEvent()
            });
        }
    }

    private void OnRemoveBatteryFinished(EntityUid uid, MechComponent component, MechRemoveBatteryFinishedEvent args)
    {
        component.EntryTokenSource = null;
        RemoveBattery(uid, component);
        _actionBlocker.UpdateCanMove(uid);
    }

    private void OnRemoveBatteryCancelled(EntityUid uid, MechComponent component, MechRemoveBatteryCancelledEvent args)
    {
        component.EntryTokenSource = null;
    }

    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        foreach (var ent in component.StartingEquipment.Select(equipment => Spawn(equipment, xform.Coordinates)))
        {
            InsertEquipment(uid, ent, component);
        }

        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        if (component.StartingBattery != null)
        {
            var battery = Spawn(component.StartingBattery, Transform(uid).Coordinates);
            InsertBattery(uid, battery, component);
        }

        _actionBlocker.UpdateCanMove(uid);
        Dirty(component);
    }

    private void OnRemoveEquipmentMessage(EntityUid uid, SharedMechComponent component, MechEquipmentRemoveMessage args)
    {
        if (!Exists(args.Equipment) || Deleted(args.Equipment))
            return;

        if (!component.EquipmentContainer.ContainedEntities.Contains(args.Equipment))
            return;

        RemoveEquipment(uid, args.Equipment, component);
    }

    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(uid, component);
    }

    private void OnToolUseAttempt(EntityUid uid, MechPilotComponent component, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancelled = true;
    }

    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Broken)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    if (component.EntryTokenSource != null)
                        return;
                    component.EntryTokenSource = new CancellationTokenSource();
                    _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.EntryDelay, component.EntryTokenSource.Token, uid)
                    {
                        BreakOnUserMove = true,
                        BreakOnStun = true,
                        TargetFinishedEvent = new MechEntryFinishedEvent(args.User),
                        TargetCancelledEvent = new MechEntryCanclledEvent()
                    });
                }
            };
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () => ToggleMechUi(uid, component, args.User),
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (!IsEmpty(component))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (component.EntryTokenSource != null)
                        return;
                    if (args.User == component.PilotSlot.ContainedEntity)
                    {
                        TryEject(uid, component);
                        return;
                    }

                    component.EntryTokenSource = new CancellationTokenSource();
                    _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.ExitDelay, component.EntryTokenSource.Token, uid)
                    {
                        BreakOnUserMove = true,
                        BreakOnTargetMove = true,
                        BreakOnStun = true,
                        TargetFinishedEvent = new MechExitFinishedEvent(),
                        TargetCancelledEvent = new MechExitCanclledEvent()
                    });
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnEntryFinished(EntityUid uid, MechComponent component, MechEntryFinishedEvent args)
    {
        component.EntryTokenSource = null;
        TryInsert(uid, args.User, component);
        _actionBlocker.UpdateCanMove(uid);
    }

    private void OnExitFinished(EntityUid uid, MechComponent component, MechExitFinishedEvent args)
    {
        component.EntryTokenSource = null;
        TryEject(uid, component);
    }

    private void OnEntryExitCancelled(EntityUid uid, MechComponent component, EntityEventArgs args)
    {
        component.EntryTokenSource = null;
    }

    private void OnDamageChanged(EntityUid uid, SharedMechComponent component, DamageChangedEvent args)
    {
        var integrity = component.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(uid, integrity, component);

        if (args.DamageIncreased &&
            args.DamageDelta != null &&
            component.PilotSlot.ContainedEntity != null)
        {
            var damage = args.DamageDelta * component.MechToPilotDamageMultiplier;
            _damageable.TryChangeDamage(component.PilotSlot.ContainedEntity, damage);
        }
    }

    private void ToggleMechUi(EntityUid uid, MechComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;
        user ??= component.PilotSlot.ContainedEntity;
        if (user == null)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
        UpdateUserInterface(uid, component);
    }

    private void RecieveEquipmentUiMesssages<T>(EntityUid uid, MechComponent component, T args) where T : MechEquipmentUiMessage
    {
        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var equipment in allEquipment)
        {
            if (args.Equipment == equipment)
                RaiseLocalEvent(equipment, ev);
        }
    }

    public override void UpdateUserInterface(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.UpdateUserInterface(uid, component);

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var ent in component.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ev);
        }

        var state = new MechBoundUiState
        {
            EquipmentStates = ev.States
        };
        var ui = _ui.GetUi(uid, MechUiKey.Key);
        _ui.SetUiState(ui, state);
    }

    public override bool TryInsert(EntityUid uid, EntityUid? toInsert, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryInsert(uid, toInsert, component))
            return false;

        var mech = (MechComponent) component;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(mech.Air, environment.RemoveVolume(MechComponent.GasMixVolume));
                }
            }
        }
        return true;
    }

    public override bool TryEject(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryEject(uid, component))
            return false;

        var mech = (MechComponent) component;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(environment, mech.Air);
                    mech.Air.Clear();
                }
            }
        }

        return true;
    }

    public override void BreakMech(EntityUid uid, SharedMechComponent? component = null)
    {
        base.BreakMech(uid, component);

        _ui.TryCloseAll(uid, MechUiKey.Key);
        _actionBlocker.UpdateCanMove(uid);
    }

    public override bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryChangeEnergy(uid, delta, component))
            return false;

        var battery = component.BatterySlot.ContainedEntity;
        if (battery == null)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        batteryComp.CurrentCharge = batteryComp.CurrentCharge + delta.Float();
        if (batteryComp.CurrentCharge != component.Energy) //if there's a discrepency, we have to resync them
        {
            _sawmill.Debug($"Battery charge was not equal to mech charge. Battery {batteryComp.CurrentCharge}. Mech {component.Energy}");
            component.Energy = batteryComp.CurrentCharge;
            Dirty(component);
        }
        _actionBlocker.UpdateCanMove(uid);
        return true;
    }

    public void InsertBattery(EntityUid uid, EntityUid toInsert, MechComponent? component = null, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!Resolve(toInsert, ref battery, false))
            return;

        component.BatterySlot.Insert(toInsert);
        component.Energy = battery.CurrentCharge;
        component.MaxEnergy = battery.MaxCharge;

        _actionBlocker.UpdateCanMove(uid);

        Dirty(component);
        UpdateUserInterface(uid, component);
    }

    public void RemoveBattery(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.EmptyContainer(component.BatterySlot);
        component.Energy = 0;
        component.MaxEnergy = 0;

        _actionBlocker.UpdateCanMove(uid);

        Dirty(component);
        UpdateUserInterface(uid, component);
    }

    #region Atmos Handling
    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExhale(EntityUid uid, MechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExpose(EntityUid uid, MechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = mech.Airtight ? mech.Air : _atmosphere.GetContainingMixture(component.Mech);

        args.Handled = true;
    }
    #endregion
}
