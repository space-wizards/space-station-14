using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Player;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }

    private void UpdateTelepad(float frameTime)
    {
        foreach (var comp in EntityManager.EntityQuery<CargoTelepadComponent>())
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(comp.Owner, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(comp.Owner, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(comp.Owner, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                continue;
            }

            var station = _station.GetOwningStation(comp.Owner);

            if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
                orderDatabase.Orders.Count == 0)
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            var orderIndices = new ValueList<int>();

            foreach (var (oIndex, oOrder) in orderDatabase.Orders)
            {
                if (!oOrder.Approved) continue;
                orderIndices.Add(oIndex);
            }

            if (orderIndices.Count == 0)
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            orderIndices.Sort();
            var index = orderIndices[0];
            var order = orderDatabase.Orders[index];
            order.Amount--;

            if (order.Amount <= 0)
                orderDatabase.Orders.Remove(index);

            _audio.PlayPvs(_audio.GetSound(comp.TeleportSound), comp.Owner, AudioParams.Default.WithVolume(-8f));
            SpawnProduct(comp, order);
            UpdateOrders(orderDatabase);

            comp.CurrentState = CargoTelepadState.Teleporting;
            _appearance.SetData(comp.Owner, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, CargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureReceiverPorts(uid, telepad.ReceiverPort);
    }

    private void SetEnabled(CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(component.Owner, ref receiver, ref xform, false)) return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled) return;

        TryComp<AppearanceComponent>(component.Owner, out var appearance);
        component.CurrentState = CargoTelepadState.Unpowered;
        _appearance.SetData(component.Owner, CargoTelepadVisuals.State, CargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(component);
    }

    /// <summary>
    ///     Spawn the product and a piece of paper. Attempt to attach the paper to the product.
    /// </summary>
    private void SpawnProduct(CargoTelepadComponent component, CargoOrderData data)
    {
        // spawn the order
        if (!_protoMan.TryIndex(data.ProductId, out CargoProductPrototype? prototype))
            return;

        var xform = Transform(component.Owner);

        var product = EntityManager.SpawnEntity(prototype.Product, xform.Coordinates);

        Transform(product).Anchored = false;

        // spawn a piece of paper.
        var printed = EntityManager.SpawnEntity(component.PrinterOutput, xform.Coordinates);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        // fill in the order data
        var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", data.PrintableOrderNumber));

        MetaData(printed).EntityName = val;

        _paperSystem.SetContent(printed, Loc.GetString(
            "cargo-console-paper-print-text",
            ("orderNumber", data.PrintableOrderNumber),
            ("itemName", prototype.Name),
            ("requester", data.Requester),
            ("reason", data.Reason),
            ("approver", data.Approver ?? string.Empty)),
            paper);

        // attempt to attach the label
        if (TryComp<PaperLabelComponent>(product, out var label))
        {
            _slots.TryInsert(product, label.LabelSlot, printed, null);
        }
    }
}
