using Content.Server.Power.Components;
using Content.Server.Emp;
using Content.Server.PowerCell;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Emp;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;
using Content.Shared.Whitelist;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class ChargerSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChargerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChargerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ChargerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChargerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ChargerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ChargerComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);
        SubscribeLocalEvent<ChargerComponent, ExaminedEvent>(OnChargerExamine);

        SubscribeLocalEvent<ChargerComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnStartup(EntityUid uid, ChargerComponent component, ComponentStartup args)
    {
        UpdateStatus(uid, component);
    }

    private void OnChargerExamine(EntityUid uid, ChargerComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ChargerComponent)))
        {
            // rate at which the charger charges
            args.PushMarkup(Loc.GetString("charger-examine", ("color", "yellow"), ("chargeRate", (int) component.ChargeRate)));

            // try to get contents of the charger
            if (!_container.TryGetContainer(uid, component.SlotId, out var container))
                return;

            if (HasComp<PowerCellSlotComponent>(uid))
                return;

            // if charger is empty and not a power cell type charger, add empty message
            // power cells have their own empty message by default, for things like flash lights
            if (container.ContainedEntities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("charger-empty"));
            }
            else
            {
                // add how much each item is charged it 
                foreach (var contained in container.ContainedEntities)
                {
                    if (!TryComp<BatteryComponent>(contained, out var battery))
                        continue;

                    var chargePercentage = (battery.CurrentCharge / battery.MaxCharge) * 100;
                    args.PushMarkup(Loc.GetString("charger-content", ("chargePercentage", (int) chargePercentage)));
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveChargerComponent, ChargerComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out _, out var charger, out var containerComp))
        {
            if (!_container.TryGetContainer(uid, charger.SlotId, out var container, containerComp))
                continue;

            if (charger.Status == CellChargerStatus.Empty || charger.Status == CellChargerStatus.Charged || container.ContainedEntities.Count == 0)
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                TransferPower(uid, contained, charger, frameTime);
            }
        }
    }

    private void OnPowerChanged(EntityUid uid, ChargerComponent component, ref PowerChangedEvent args)
    {
        UpdateStatus(uid, component);
    }

    private void OnInserted(EntityUid uid, ChargerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.SlotId)
            return;

        UpdateStatus(uid, component);
    }

    private void OnRemoved(EntityUid uid, ChargerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.SlotId)
            return;

        UpdateStatus(uid, component);
    }

    /// <summary>
    ///     Verify that the entity being inserted is actually rechargeable.
    /// </summary>
    private void OnInsertAttempt(EntityUid uid, ChargerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.SlotId)
            return;

        if (!TryComp<PowerCellSlotComponent>(args.EntityUid, out var cellSlot))
            return;

        if (!cellSlot.FitsInCharger)
            args.Cancel();
    }

    private void OnEntityStorageInsertAttempt(EntityUid uid, ChargerComponent component, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (!component.Initialized || args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlot))
            return;

        if (!cellSlot.FitsInCharger)
            args.Cancelled = true;
    }

    private void UpdateStatus(EntityUid uid, ChargerComponent component)
    {
        var status = GetStatus(uid, component);
        TryComp(uid, out AppearanceComponent? appearance);

        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return;

        _appearance.SetData(uid, CellVisual.Occupied, container.ContainedEntities.Count != 0, appearance);
        if (component.Status == status || !TryComp(uid, out ApcPowerReceiverComponent? receiver))
            return;

        component.Status = status;

        if (component.Status == CellChargerStatus.Charging)
        {
            AddComp<ActiveChargerComponent>(uid);
        }
        else
        {
            RemComp<ActiveChargerComponent>(uid);
        }

        switch (component.Status)
        {
            case CellChargerStatus.Off:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Off, appearance);
                break;
            case CellChargerStatus.Empty:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Empty, appearance);
                break;
            case CellChargerStatus.Charging:
                receiver.Load = component.ChargeRate;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Charging, appearance);
                break;
            case CellChargerStatus.Charged:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Charged, appearance);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnEmpPulse(EntityUid uid, ChargerComponent component, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
    }

    private CellChargerStatus GetStatus(EntityUid uid, ChargerComponent component)
    {
        if (!component.Portable)
        {
            if (!TryComp(uid, out TransformComponent? transformComponent) || !transformComponent.Anchored)
                return CellChargerStatus.Off;
        }

        if (!TryComp(uid, out ApcPowerReceiverComponent? apcPowerReceiverComponent))
            return CellChargerStatus.Off;

        if (!component.Portable && !apcPowerReceiverComponent.Powered)
            return CellChargerStatus.Off;

        if (HasComp<EmpDisabledComponent>(uid))
            return CellChargerStatus.Off;

        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return CellChargerStatus.Off;

        if (container.ContainedEntities.Count == 0)
            return CellChargerStatus.Empty;

        if (!SearchForBattery(container.ContainedEntities[0], out _, out var heldBattery))
            return CellChargerStatus.Off;

        if (Math.Abs(heldBattery.MaxCharge - heldBattery.CurrentCharge) < 0.01)
            return CellChargerStatus.Charged;

        return CellChargerStatus.Charging;
    }
    
    private void TransferPower(EntityUid uid, EntityUid targetEntity, ChargerComponent component, float frameTime)
    {
        if (!TryComp(uid, out ApcPowerReceiverComponent? receiverComponent))
            return;

        if (!receiverComponent.Powered)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, targetEntity))
            return;

        if (!SearchForBattery(targetEntity, out var batteryUid, out var heldBattery))
            return;

        _battery.SetCharge(batteryUid.Value, heldBattery.CurrentCharge + component.ChargeRate * frameTime, heldBattery);
        // Just so the sprite won't be set to 99.99999% visibility
        if (heldBattery.MaxCharge - heldBattery.CurrentCharge < 0.01)
        {
            _battery.SetCharge(batteryUid.Value, heldBattery.MaxCharge, heldBattery);
        }

        UpdateStatus(uid, component);
    }

    private bool SearchForBattery(EntityUid uid, [NotNullWhen(true)] out EntityUid? batteryUid, [NotNullWhen(true)] out BatteryComponent? component)
    {
        // try get a battery directly on the inserted entity
        if (!TryComp(uid, out component))
        {
            // or by checking for a power cell slot on the inserted entity
            return _powerCell.TryGetBatteryFromSlot(uid, out batteryUid, out component);
        }
        batteryUid = uid;
        return true;
    }
}
