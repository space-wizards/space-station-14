using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CargoTelepadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
        SubscribeLocalEvent<FulfillCargoOrderEvent>(OnTelepadFulfillCargoOrder);
    }

    private void OnTelepadFulfillCargoOrder(ref FulfillCargoOrderEvent args)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tele, out var xform))
        {
            if (tele.CurrentState != CargoTelepadState.Idle)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (_station.GetOwningStation(uid, xform) != args.Station)
                continue;

            if (!IsLinkedToConsole(uid, GetEntity(args.Order.ApprovingConsole)))
                continue;

            tele.CurrentOrders.Add(args.Order);

            tele.Accumulator = tele.Delay;
            args.Handled = true;
            args.FulfillmentEntity = uid;
            return;
        }
    }

    private bool IsLinkedToConsole(
        EntityUid uid,
        EntityUid? approvingConsole
    )
    {
        if (approvingConsole == null)
            return false;

        if (!TryGetLinkedConsoles(uid, out var consoles))
            return false;

        return consoles.Any(console => console.Owner == approvingConsole);
    }

    private bool TryGetLinkedConsoles(
        EntityUid uid,
        [NotNullWhen(true)] out List<Entity<CargoOrderConsoleComponent>>? consoles
    )
    {
        consoles = new();
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sinkComponent))
        {
            consoles = null;
            return false;
        }

        consoles = new();
        foreach (var linked in sinkComponent.LinkedSources)
        {
            if (!TryComp<CargoOrderConsoleComponent>(linked, out var consoleComp))
                continue;
            consoles.Add((linked, consoleComp));
        }

        return consoles.Count > 0;
    }

    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(uid, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered)
            {
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                continue;
            }

            if (comp.CurrentOrders.Count == 0 || !TryGetLinkedConsoles(uid, out var consoles))
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            var currentOrder = comp.CurrentOrders.First();
            if (currentOrder.NumDispatched >= currentOrder.OrderQuantity)
            {
                comp.CurrentOrders.Remove(currentOrder);
            }
            else if (FulfillOrder(currentOrder, currentOrder.Account, xform.Coordinates, comp.PrinterOutput))
            {
                currentOrder.NumDispatched++;
                if (currentOrder.NumDispatched >= currentOrder.OrderQuantity)
                    comp.CurrentOrders.Remove(currentOrder);

                var teleportSound = comp.TeleportSound;
                var audioParams = teleportSound?.Params ?? AudioParams.Default;
                audioParams = audioParams.AddVolume(-8f);
                _audio.PlayPvs(_audio.ResolveSound(comp.TeleportSound), uid, audioParams);

                if (_station.GetOwningStation(uid) is { } station)
                    UpdateOrders(station);

                comp.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, CargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureSinkPorts(uid, telepad.ReceiverPort);
    }

    private void OnShutdown(Entity<CargoTelepadComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentOrders.Count == 0)
            return;

        if (_station.GetStations().Count == 0)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
        {
            station = _random.Pick(_station.GetStations().Where(HasComp<StationCargoOrderDatabaseComponent>).ToList());
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var db) ||
            !TryComp<StationDataComponent>(station, out var data))
            return;

        foreach (var order in ent.Comp.CurrentOrders)
        {
            TryFulfillOrder((station, data), order.Account, order, db);
        }
    }

    private void SetEnabled(EntityUid uid, CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(uid, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Turn off if disabled
        // Only change to Idle if off
        // don't overwrite teleporting state
        if (disabled)
            component.CurrentState = CargoTelepadState.Unpowered;
        else if (component.CurrentState == CargoTelepadState.Unpowered)
            component.CurrentState = CargoTelepadState.Idle;

        _appearance.SetData(uid, CargoTelepadVisuals.State, component.CurrentState);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(uid, component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(uid, component);
    }
}
