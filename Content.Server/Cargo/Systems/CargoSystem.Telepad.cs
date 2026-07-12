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
    [SubscribeLocalEvent]
    private void OnTelepadFulfillCargoOrder(ref FulfillCargoOrderEvent args)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var telepad, out var xform))
        {
            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (_station.GetOwningStation(uid, xform) != args.Station)
                continue;

            if (!IsLinkedToConsole(uid, GetEntity(args.Order.ApprovingConsole)))
                continue;

            telepad.CurrentOrders.Add(args.Order);

            args.Handled = true;
            args.FulfillmentEntity = uid;
            return;
        }
    }

    private bool IsLinkedToConsole(
        EntityUid uid,
        EntityUid? approvingConsole,
        List<Entity<CargoOrderConsoleComponent>>? consoles = null
    )
    {
        if (approvingConsole == null)
            return false;

        if (consoles == null && !TryGetLinkedConsoles(uid, out consoles))
            return false;

        return consoles.Any(console => console.Owner == approvingConsole);
    }

    private bool TryGetLinkedConsoles(
        EntityUid uid,
        [NotNullWhen(true)] out List<Entity<CargoOrderConsoleComponent>> consoles
    )
    {
        consoles = new();
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sinkComponent))
            return false;
        foreach (var linked in sinkComponent.LinkedSources)
        {
            if (!TryComp<CargoOrderConsoleComponent>(linked, out var consoleComp))
                continue;
            consoles.Add((linked, consoleComp));
        }
        return consoles.Count > 0;
    }

    private void UpdateTelepad()
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var telepad, out var xform))
        {
            if (telepad.CurrentState == CargoTelepadState.Unpowered)
                continue;

            if (Timing.CurTime < telepad.NextTeleport)
            {
                telepad.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle);
                continue;
            }

            // Not done using += TeleportDelay as this is not guaranteed to run every time.
            // Need to avoid teleporting many crates at once because NextTeleport lagged behind while unpowered.
            telepad.NextTeleport = Timing.CurTime + telepad.TeleportDelay;

            telepad.CurrentOrders.RemoveAll(order => order.NumDispatched >= order.OrderQuantity);

            // Will only run every delay. Can use TryComps here without problem
            if (telepad.CurrentOrders.Count == 0 || !TryGetLinkedConsoles(uid, out var consoles))
                continue;

            var currentOrder = telepad.CurrentOrders.First();

            if (FulfillOrder(currentOrder, currentOrder.Account, xform.Coordinates, telepad.PrinterOutput))
            {
                currentOrder.NumDispatched++;
                _audio.PlayPvs(_audio.ResolveSound(telepad.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));

                if (_station.GetOwningStation(uid) is { } station)
                    UpdateOrders(station);

                telepad.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<CargoTelepadComponent> ent, ref ComponentInit args)
    {
        _linker.EnsureSinkPorts(ent.Owner, ent.Comp.ReceiverPort);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CargoTelepadComponent> ent, ref ComponentShutdown args)
    {
        // Orders should not be this fragile
        if (ent.Comp.CurrentOrders.Count == 0)
            return;

        if (_station.GetStations().Count == 0)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
        {
            station = _random.Pick(_station.GetStations().Where(HasComp<StationCargoOrderDatabaseComponent>).ToList());
        }

        if (
            !TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase)
            || !TryComp<StationDataComponent>(station, out var stationData)
        )
            return;

        foreach (var order in ent.Comp.CurrentOrders)
        {
            TryFulfillOrder((station, stationData), order.Account, order, orderDatabase);
        }
    }

    private void CheckEnabled(
        Entity<CargoTelepadComponent> ent,
        ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null
    )
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(ent.Owner, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Turn off if disabled
        // Only change to Idle if off; don't overwrite teleporting state
        if (disabled)
            ent.Comp.CurrentState = CargoTelepadState.Unpowered;
        else if (ent.Comp.CurrentState == CargoTelepadState.Unpowered)
            ent.Comp.CurrentState = CargoTelepadState.Idle;

        _appearance.SetData(ent.Owner, CargoTelepadVisuals.State, ent.Comp.CurrentState);
    }

    [SubscribeLocalEvent]
    private void OnTelepadPowerChange(Entity<CargoTelepadComponent> ent, ref PowerChangedEvent args)
    {
        CheckEnabled(ent);
    }

    [SubscribeLocalEvent]
    private void OnTelepadAnchorChange(Entity<CargoTelepadComponent> ent, ref AnchorStateChangedEvent args)
    {
        CheckEnabled(ent);
    }
}
