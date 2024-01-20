using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Hands.Systems;
using Content.Server.PowerCell;
using Content.Server.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    [ValidatePrototypeId<JobPrototype>]
    public const string BorgJobId = "Borg";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
        SubscribeLocalEvent<BorgChassisComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BorgChassisComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<BorgChassisComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BorgChassisComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<BorgChassisComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<BorgChassisComponent, GetCharactedDeadIcEvent>(OnGetDeadIC);

        SubscribeLocalEvent<BorgBrainComponent, MindAddedMessage>(OnBrainMindAdded);

        InitializeModules();
        InitializeMMI();
        InitializeUI();
    }

    private void OnMapInit(EntityUid uid, BorgChassisComponent component, MapInitEvent args)
    {
        UpdateBatteryAlert(uid);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnChassisInteractUsing(EntityUid uid, BorgChassisComponent component, AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Handled || uid == args.User)
            return;

        var used = args.Used;
        TryComp<BorgBrainComponent>(used, out var brain);
        TryComp<BorgModuleComponent>(used, out var module);

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
        {
            if (brain != null || module != null)
            {
                Popup.PopupEntity(Loc.GetString("borg-panel-not-open"), uid, args.User);
            }
            return;
        }

        if (component.BrainEntity == null &&
            brain != null &&
            component.BrainWhitelist?.IsValid(used) != false)
        {
            if (_mind.TryGetMind(used, out _, out var mind) && mind.Session != null)
            {
                if (!CanPlayerBeBorged(mind.Session))
                {
                    Popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), used, args.User);
                    return;
                }
            }

            _container.Insert(used, component.BrainContainer);
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User):player} installed brain {ToPrettyString(used)} into borg {ToPrettyString(uid)}");
            args.Handled = true;
            UpdateUI(uid, component);
        }

        if (module != null && CanInsertModule(uid, used, component, module, args.User))
        {
            _container.Insert(used, component.ModuleContainer);
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(args.User):player} installed module {ToPrettyString(used)} into borg {ToPrettyString(uid)}");
            args.Handled = true;
            UpdateUI(uid, component);
        }
    }

    // todo: consider transferring over the ghost role? managing that might suck.
    protected override void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {
        base.OnInserted(uid, component, args);

        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(args.Entity, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, uid, mind: mind);
        }
    }

    protected override void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {
        base.OnRemoved(uid, component, args);

        if (HasComp<BorgBrainComponent>(args.Entity) &
            _mind.TryGetMind(uid, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, args.Entity, mind: mind);
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

    private void OnMobStateChanged(EntityUid uid, BorgChassisComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            if (_mind.TryGetMind(uid, out _, out _))
                _powerCell.SetPowerCellDrawEnabled(uid, true);
        }
        else
        {
            _powerCell.SetPowerCellDrawEnabled(uid, false);
        }
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
            if (!draw.Drawing && _mind.TryGetMind(uid, out _, out _) && _mobState.IsAlive(uid))
                _powerCell.SetPowerCellDrawEnabled(uid, true);

            EnableBorgAbilities(uid, component);
        }

        UpdateUI(uid, component);
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, BorgChassisComponent component, ref PowerCellSlotEmptyEvent args)
    {
        DisableBorgAbilities(uid, component);
        UpdateUI(uid, component);
    }

    private void OnUIOpenAttempt(EntityUid uid, BorgChassisComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // borgs can't view their own ui
        if (args.User == uid)
            args.Cancel();
    }

    private void OnGetDeadIC(EntityUid uid, BorgChassisComponent component, ref GetCharactedDeadIcEvent args)
    {
        args.Dead = true;
    }

    private void OnBrainMindAdded(EntityUid uid, BorgBrainComponent component, MindAddedMessage args)
    {
        if (!Container.TryGetOuterContainer(uid, Transform(uid), out var container))
            return;

        var containerEnt = container.Owner;

        if (!TryComp<BorgChassisComponent>(containerEnt, out var chassisComponent) ||
            container.ID != chassisComponent.BrainContainerId)
            return;

        if (!_mind.TryGetMind(uid, out var mindId, out var mind) || mind.Session == null)
            return;

        if (!CanPlayerBeBorged(mind.Session))
        {
            Popup.PopupEntity(Loc.GetString("borg-player-not-allowed-eject"), uid);
            Container.RemoveEntity(containerEnt, uid);
            _throwing.TryThrow(uid, _random.NextVector2() * 5, 5f);
            return;
        }

        _mind.TransferTo(mindId, containerEnt, mind: mind);
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
        if (chargePercent == 0 && _powerCell.HasDrawCharge(uid, cell: slotComponent))
        {
            chargePercent = 1;
        }

        _alerts.ClearAlert(uid, AlertType.BorgBatteryNone);
        _alerts.ShowAlert(uid, AlertType.BorgBattery, chargePercent);
    }

    /// <summary>
    /// Activates the borg, enabling all of its modules.
    /// </summary>
    public void EnableBorgAbilities(EntityUid uid, BorgChassisComponent component, PowerCellDrawComponent? powerCell = null)
    {
        if (component.Activated)
            return;

        component.Activated = true;
        InstallAllModules(uid, component);
        Dirty(component);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Deactivates the borg, disabling all of its modules and decreasing its speed.
    /// </summary>
    public void DisableBorgAbilities(EntityUid uid, BorgChassisComponent component)
    {
        if (!component.Activated)
            return;

        component.Activated = false;
        DisableAllModules(uid, component);
        Dirty(component);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Activates a borg when a player occupies it
    /// </summary>
    public void BorgActivate(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-added", ("name", Identity.Name(uid, EntityManager))), uid);
        _powerCell.SetPowerCellDrawEnabled(uid, true);
        _access.SetAccessEnabled(uid, true);
        _appearance.SetData(uid, BorgVisuals.HasPlayer, true);
        Dirty(uid, component);
    }

    /// <summary>
    /// Deactivates a borg when a player leaves it.
    /// </summary>
    public void BorgDeactivate(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(uid, EntityManager))), uid);
        _powerCell.SetPowerCellDrawEnabled(uid, false);
        _access.SetAccessEnabled(uid, false);
        _appearance.SetData(uid, BorgVisuals.HasPlayer, false);
        Dirty(uid, component);
    }

    /// <summary>
    /// Checks that a player has fulfilled the requirements for the borg job.
    /// If they don't have enough hours, they cannot be placed into a chassis.
    /// </summary>
    public bool CanPlayerBeBorged(ICommonSession session)
    {
        if (_banManager.GetJobBans(session.UserId)?.Contains(BorgJobId) == true)
            return false;

        return true;
    }
}
