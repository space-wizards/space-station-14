using Content.Server.Actions;
using Content.Server.Administration.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.PowerCell;
using Content.Server.UserInterface;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
        SubscribeLocalEvent<BorgChassisComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BorgChassisComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<BorgChassisComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);

        SubscribeLocalEvent<BorgBrainComponent, MindAddedMessage>(OnBrainMindAdded);

        InitializeModules();
        InitializeMMI();
    }

    private void OnMapInit(EntityUid uid, BorgChassisComponent component, MapInitEvent args)
    {
        UpdateBatteryAlert(uid);
    }

    private void OnChassisInteractUsing(EntityUid uid, BorgChassisComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled || uid == args.User)
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
        {
            //todo: if you're using a module, brain, or leg: show a popup to open the panel
            return;
        }

        args.Handled = true;
        var used = args.Used;

        if (component.BrainEntity == null &&
            HasComp<BorgBrainComponent>(used) &&
            component.BrainWhitelist?.IsValid(used) != false)
        {
            if (_mind.TryGetMind(used, out var mind) && mind.Session != null)
            {
                if (!CanPlayerBeBorgged(mind.Session, component))
                {
                    Popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), used, args.User);
                    return;
                }
            }

            component.BrainContainer.Insert(used);
        }

        if (TryComp<BorgModuleComponent>(used, out var module) &&
            CanInsertModule(uid, used, component, module))
        {
            component.ModuleContainer.Insert(used);
        }
    }

    // todo: consider transferring over the ghost role? managing that might suck.
    protected override void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {
        base.OnInserted(uid, component, args);

        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(args.Entity, out var mind))
        {
            _mind.TransferTo(mind, uid);
        }
    }

    protected override void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {
        base.OnRemoved(uid, component, args);

        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(uid, out var mind))
        {
            _mind.TransferTo(mind, args.Entity);
        }
    }

    private void OnMindAdded(EntityUid uid, BorgChassisComponent component, MindAddedMessage args)
    {
        BorgActivate(uid, component);
    }

    private void OnMindRemoved(EntityUid uid, BorgChassisComponent component, MindRemovedMessage args)
    {
        BorgDeactivate(uid, component);
    }

    private void OnPowerCellChanged(EntityUid uid, BorgChassisComponent component, PowerCellChangedEvent args)
    {
        UpdateBatteryAlert(uid);

        if (!TryComp<PowerCellDrawComponent>(uid, out var draw))
            return;

        // if we eject the battery or run out of charge, then disable
        if (args.Ejected || !_powerCell.HasDrawCharge(uid))
        {
            DisableBorgAbilities(uid, component);
            return;
        }

        // if we aren't drawing and suddenly get enough power to draw again, reeanble.
        if (_powerCell.HasDrawCharge(uid, draw))
        {
            // only reenable the powerdraw if a player has the role.
            if (!draw.Drawing && _mind.TryGetMind(uid, out _))
                _powerCell.SetPowerCellDrawEnabled(uid, true);

            EnableBorgAbilities(uid, component);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, BorgChassisComponent component, ref PowerCellSlotEmptyEvent args)
    {
        DisableBorgAbilities(uid, component);
    }

    private void OnUIOpenAttempt(EntityUid uid, BorgChassisComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // borgs can't view their own ui
        if (args.User == uid)
            args.Cancel();
    }

    private void OnBrainMindAdded(EntityUid uid, BorgBrainComponent component, MindAddedMessage args)
    {
        if (!Container.TryGetOuterContainer(uid, Transform(uid), out var container))
            return;

        var containerEnt = container.Owner;

        if (!TryComp<BorgChassisComponent>(containerEnt, out var chassisComponent) ||
            container.ID != chassisComponent.BrainContainerId)
            return;

        if (!_mind.TryGetMind(uid, out var mind) || mind.Session == null)
            return;

        if (!CanPlayerBeBorgged(mind.Session, chassisComponent))
        {
            Popup.PopupEntity(Loc.GetString("borg-player-not-allowed-eject"), uid);
            Container.RemoveEntity(containerEnt, uid);
            _throwing.TryThrow(uid, _random.NextVector2() * 5, 5f);
            return;
        }

        _mind.TransferTo(mind, containerEnt);
    }

    private void UpdateBatteryAlert(EntityUid uid, PowerCellSlotComponent? slotComponent = null)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery, slotComponent))
        {
            _alerts.ClearAlert(uid, AlertType.BorgBattery);
            _alerts.ShowAlert(uid, AlertType.BorgBatteryNone);
            return;
        }

        var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        // we make sure 0 only shows if they have absolutely no battery.
        // also account for floating point imprecision
        if (chargePercent == 0 && battery.CurrentCharge >= 0.01)
        {
            chargePercent = 1;
        }

        _alerts.ClearAlert(uid, AlertType.BorgBatteryNone);
        _alerts.ShowAlert(uid, AlertType.BorgBattery, chargePercent);
    }

    public void EnableBorgAbilities(EntityUid uid, BorgChassisComponent component)
    {
        if (component.Activated)
            return;

        component.Activated = true;
        EnableAllModules(uid, component);
        Dirty(component);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    public void DisableBorgAbilities(EntityUid uid, BorgChassisComponent component)
    {
        if (!component.Activated)
            return;

        component.Activated = false;
        DisableAllModules(uid, component);
        Dirty(component);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    public void BorgActivate(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-added", ("name", Identity.Name(uid, EntityManager))), uid);
        _powerCell.SetPowerCellDrawEnabled(uid, true);
        _appearance.SetData(uid, BorgVisuals.HasPlayer, true);
    }

    public void BorgDeactivate(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(uid, EntityManager))), uid);
        _powerCell.SetPowerCellDrawEnabled(uid, false);
        _appearance.SetData(uid, BorgVisuals.HasPlayer, false);
    }

    /// <summary>
    /// Checks that a player has fulfilled the requirements for the borg job.
    /// If they don't have enough hours, they cannot be placed into a chassis.
    /// </summary>
    public bool CanPlayerBeBorgged(IPlayerSession session, BorgChassisComponent component)
    {
        var disallowedJobs = _playTimeTracking.GetDisallowedJobs(session);

        if (disallowedJobs.Contains(component.BorgJobId))
            return false;

        if (_banManager.GetJobBans(session.UserId)?.Contains(component.BorgJobId) == true)
            return false;

        return true;
    }
}
