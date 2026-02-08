using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Mech.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles logic for the mech interface.
/// </summary>
/// <remarks>
/// This system is responsible for updating the mech UI state and handling UI interactions.
/// It is not responsible for any mech logic on its own, it merely provides UI functionality.
/// </remarks>
public sealed class MechInterfaceSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MechLockSystem _mechLock = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, UpdateMechUiEvent>(OnUpdateMechUi);

        Subs.BuiEvents<MechComponent>(MechUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnMechUiOpened);
            });

        Subs.BuiEvents<MechComponent>(
            MechUiKey.Key,
            subs =>
            {
                subs.Event<MechEquipmentRemoveMessage>(HandleEquipmentRemove);
                subs.Event<MechModuleRemoveMessage>(HandleModuleRemove);
                subs.Event<MechCabinAirMessage>(HandleCabinPurge);

                subs.Event<MechDnaLockRegisterMessage>(HandleDnaLockRegister);
                subs.Event<MechDnaLockToggleMessage>(HandleDnaLockToggle);
                subs.Event<MechDnaLockResetMessage>(HandleDnaLockReset);
                subs.Event<MechCardLockRegisterMessage>(HandleCardLockRegister);
                subs.Event<MechCardLockToggleMessage>(HandleCardLockToggle);
                subs.Event<MechCardLockResetMessage>(HandleCardLockReset);

                subs.Event<MechEquipmentUiMessage>(HandleEquipmentUiMessageRelay);
                subs.Event<MechGrabberEjectMessage>(HandleEquipmentUiMessageRelay);
                subs.Event<MechSoundboardPlayMessage>(HandleEquipmentUiMessageRelay);
                subs.Event<MechGeneratorEjectFuelMessage>(HandleEquipmentUiMessageRelay);
            });
    }

    private void OnMechUiOpened(Entity<MechComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnUpdateMechUi(Entity<MechComponent> ent, ref UpdateMechUiEvent args)
    {
        UpdateUi(ent);
    }

    private void RelayEquipmentUiMessage(MechEquipmentUiMessage msg)
    {
        var equipment = GetEntity(msg.Equipment);
        RaiseLocalEvent(equipment, new MechEquipmentUiMessageRelayEvent(msg));
    }

    private void HandleEquipmentUiMessageRelay(Entity<MechComponent> ent, ref MechEquipmentUiMessage args)
    {
        RelayEquipmentUiMessage(args);
    }

    private void HandleEquipmentUiMessageRelay(Entity<MechComponent> ent, ref MechGrabberEjectMessage args)
    {
        RelayEquipmentUiMessage(args);
    }

    private void HandleEquipmentUiMessageRelay(Entity<MechComponent> ent, ref MechSoundboardPlayMessage args)
    {
        RelayEquipmentUiMessage(args);
    }

    private void HandleEquipmentUiMessageRelay(Entity<MechComponent> ent, ref MechGeneratorEjectFuelMessage args)
    {
        RelayEquipmentUiMessage(args);
    }

    private void HandleEquipmentRemove(Entity<MechComponent> ent, ref MechEquipmentRemoveMessage args)
    {
        var equipment = GetEntity(args.Equipment);
        if (!ent.Comp.EquipmentContainer.Contains(equipment))
            return;

        _container.Remove(equipment, ent.Comp.EquipmentContainer);
        UpdateUi(ent);
    }

    private void HandleModuleRemove(Entity<MechComponent> ent, ref MechModuleRemoveMessage args)
    {
        var module = GetEntity(args.Module);
        if (!ent.Comp.ModuleContainer.Contains(module))
            return;

        _container.Remove(module, ent.Comp.ModuleContainer);
        UpdateUi(ent);
    }

    private void HandleCabinPurge(Entity<MechComponent> ent, ref MechCabinAirMessage args)
    {
        if (!TryComp<MechCabinAirComponent>(ent, out var cabin))
            return;

        var atmos = EntityManager.System<AtmosphereSystem>();
        var environment = atmos.GetContainingMixture(ent.Owner, false, true);
        if (environment != null)
        {
            var removed = cabin.Air.RemoveRatio(1f);
            atmos.Merge(environment, removed);
        }
        else
        {
            cabin.Air.Clear();
        }

        var purgeComp = EnsureComp<MechCabinPurgeComponent>(ent);
        purgeComp.CooldownRemaining = purgeComp.CooldownDuration;
        Dirty(ent, purgeComp);
        UpdateUi(ent);
    }

    private void HandleDnaLockRegister(Entity<MechComponent> ent, ref MechDnaLockRegisterMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechDnaLockRegisterEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void HandleDnaLockToggle(Entity<MechComponent> ent, ref MechDnaLockToggleMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechDnaLockToggleEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void HandleDnaLockReset(Entity<MechComponent> ent, ref MechDnaLockResetMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechDnaLockResetEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void HandleCardLockRegister(Entity<MechComponent> ent, ref MechCardLockRegisterMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechCardLockRegisterEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void HandleCardLockToggle(Entity<MechComponent> ent, ref MechCardLockToggleMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechCardLockToggleEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void HandleCardLockReset(Entity<MechComponent> ent, ref MechCardLockResetMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var ev = new MechCardLockResetEvent { User = GetNetEntity(args.Actor) };
        RaiseLocalEvent(ent, ev);
    }

    private void UpdateUi(Entity<MechComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, MechUiKey.Key))
            return;

        ent.Comp.LastUiUpdate = _gameTiming.CurTime;

        var equipment = new List<NetEntity>();
        foreach (var equipmentEnt in ent.Comp.EquipmentContainer.ContainedEntities)
        {
            equipment.Add(GetNetEntity(equipmentEnt));
        }

        var modules = new List<NetEntity>();
        foreach (var modulesEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            modules.Add(GetNetEntity(modulesEnt));
        }

        MechFanModuleComponent? fanModule = null;
        foreach (var modulesEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (!TryComp<MechFanModuleComponent>(modulesEnt, out var fan))
                continue;

            fanModule = fan;
        }

        var fanActive = fanModule?.IsActive ?? false;
        var fanState = fanModule?.State ?? MechFanState.Na;
        var filterEnabled = fanModule?.FilterEnabled ?? false;

        var hasFanModule = false;
        var hasGasModule = false;
        var moduleUsed = 0;
        foreach (var modulesEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (HasComp<MechFanModuleComponent>(modulesEnt))
                hasFanModule = true;
            if (HasComp<MechAirTankModuleComponent>(modulesEnt))
                hasGasModule = true;
            if (TryComp<MechModuleComponent>(modulesEnt, out var m))
                moduleUsed += m.Size;
        }

        var cabinPressure = 0f;
        var cabinTemperature = 0f;
        var gasAmountLiters = 0f;
        var tankPressure = 0f;
        if (TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
        {
            cabinPressure = cabin.Air.Pressure;
            cabinTemperature = cabin.Air.Temperature;
        }

        GasMixture? tankAir = null;
        foreach (var modulesEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (HasComp<MechAirTankModuleComponent>(modulesEnt))
                continue;

            if (!TryComp<GasTankComponent>(modulesEnt, out var tank))
                continue;

            tankAir = tank.Air;
        }

        if (tankAir != null)
        {
            // Pressure straight from tank and amount in liters.
            tankPressure = tankAir.Pressure;
            var pressure = MathF.Max(tankAir.Pressure, 0f);
            if (pressure > 0)
                gasAmountLiters = tankAir.TotalMoles * Atmospherics.R * tankAir.Temperature / pressure;
        }

        // Compute energy from battery.
        var energy = 0f;
        var maxEnergy = 0f;
        if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            energy = _battery.GetCharge(battery.Value.AsNullable());
            maxEnergy = battery.Value.Comp.MaxCharge;
        }

        var purgeAvailable = false;
        if (TryComp<MechCabinPurgeComponent>(ent.Owner, out var purgeComp))
            purgeAvailable = purgeComp.CooldownRemaining <= 0;

        var state = new MechBoundUiState
        {
            Equipment = equipment,
            Modules = modules,
            IsAirtight = ent.Comp.Airtight,
            FanActive = fanActive,
            FanState = fanState,
            FilterEnabled = filterEnabled,
            CabinPressureLevel = cabinPressure,
            CabinTemperature = cabinTemperature,
            GasAmountLiters = gasAmountLiters,
            TankPressure = tankPressure,
            HasFanModule = hasFanModule,
            HasGasModule = hasGasModule,
            ModuleSpaceMax = ent.Comp.MaxModuleAmount,
            ModuleSpaceUsed = moduleUsed,
            PilotPresent = ent.Comp.PilotSlot.ContainedEntity != null,
            Integrity = ent.Comp.Integrity.Float(),
            MaxIntegrity = ent.Comp.MaxIntegrity.Float(),
            Energy = energy,
            MaxEnergy = maxEnergy,
            CanAirtight = ent.Comp.CanAirtight,
            EquipmentUsed = ent.Comp.EquipmentContainer.ContainedEntities.Count,
            MaxEquipmentAmount = ent.Comp.MaxEquipmentAmount,
            IsBroken = ent.Comp.Broken,
            CabinPurgeAvailable = purgeAvailable,
        };

        if (TryComp<MechLockComponent>(ent.Owner, out var lockComp))
        {
            state.DnaLockRegistered = lockComp.DnaLockRegistered;
            state.DnaLockActive = lockComp.DnaLockActive;
            state.CardLockRegistered = lockComp.CardLockRegistered;
            state.CardLockActive = lockComp.CardLockActive;
            state.OwnerDna = lockComp.OwnerDna;
            state.OwnerJobTitle = lockComp.OwnerJobTitle;
            state.IsLocked = lockComp.IsLocked;
        }

        // Collect equipment and module UI states.
        CollectEquipmentUiStates(ent.Comp.EquipmentContainer.ContainedEntities, state.EquipmentUiStates);
        CollectEquipmentUiStates(ent.Comp.ModuleContainer.ContainedEntities, state.EquipmentUiStates);

        _uiSystem.SetUiState(ent.Owner, MechUiKey.Key, state);
    }

    private void CollectEquipmentUiStates(IEnumerable<EntityUid> entities,
        Dictionary<NetEntity, BoundUserInterfaceState> states)
    {
        foreach (var entity in entities)
        {
            var ev = new MechEquipmentUiStateReadyEvent();
            RaiseLocalEvent(entity, ev);
            if (ev.States.Count == 0)
                continue;

            foreach (var (netEntity, state) in ev.States)
            {
                states[netEntity] = state;
            }
        }
    }
}
