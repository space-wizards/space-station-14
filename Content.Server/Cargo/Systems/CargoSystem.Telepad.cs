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
    private void OnInit(Entity<CargoTelepadComponent> ent, ref ComponentInit args)
    {
        _linker.EnsureSinkPorts(ent.Owner, ent.Comp.ReceiverPort);
    }

    [SubscribeLocalEvent]
    private void OnTelepadPowerChange(Entity<CargoTelepadComponent> ent, ref PowerChangedEvent args)
    {
        SetEnabled(ent);
    }

    [SubscribeLocalEvent]
    private void OnTelepadAnchorChange(Entity<CargoTelepadComponent> ent, ref AnchorStateChangedEvent args)
    {
        SetEnabled(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CargoTelepadComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentOrders.Count == 0
            || _station.GetStations().Count == 0)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
        {
            station = _random.Pick(_station.GetStations().Where(x => HasComp<StationCargoOrderDatabaseComponent>(x.Owner)).ToList());
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase)
            || !TryComp<StationDataComponent>(station, out var data))
            return;

        foreach (var order in ent.Comp.CurrentOrders)
        {
            TryFulfillOrder((station, data), order.Account, order, orderDatabase);
        }
    }

    [SubscribeLocalEvent]
    private void OnTelepadFulfillCargoOrder(ref FulfillCargoOrderEvent args)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var telepad, out var xform))
        {
            if (telepad.CurrentState != CargoTelepadState.Idle
                || !this.IsPowered(uid, EntityManager)
                || _station.GetOwningStation(uid, xform) != args.Station
                || !IsLinkedToConsole(uid, GetEntity(args.Order.ApprovingConsole)))
                continue;

            telepad.NextTeleport = Timing.CurTime + telepad.Delay;
            telepad.CurrentOrders.Add(args.Order);
            args.Handled = true;
            args.FulfillmentEntity = uid;
            return;
        }
    }

    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var telepad, out var xform))
        {
            // Uhh listen teleporting takes time and I just want the 1 float.
            if (Timing.CurTime < telepad.NextTeleport)
            {
                telepad.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle);
                continue;
            }

            if (telepad.CurrentOrders.Count == 0)
                continue;

            telepad.NextTeleport = Timing.CurTime + telepad.Delay;

            var currentOrder = telepad.CurrentOrders.First();
            if (currentOrder.NumDispatched >= currentOrder.OrderQuantity)
            {
                telepad.CurrentOrders.Remove(currentOrder);
            }
            else if (FulfillOrder(currentOrder, currentOrder.Account, xform.Coordinates, telepad.PrinterOutput))
            {
                currentOrder.NumDispatched++;
                if (currentOrder.NumDispatched >= currentOrder.OrderQuantity)
                    telepad.CurrentOrders.Remove(currentOrder);

                var teleportSound = telepad.TeleportSound;
                var audioParams = teleportSound?.Params ?? AudioParams.Default;
                audioParams = audioParams.AddVolume(-8f);
                _audio.PlayPvs(_audio.ResolveSound(telepad.TeleportSound), uid, audioParams);

                if (_station.GetOwningStation(uid) is { } station)
                    UpdateOrders(station);

                telepad.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting);
            }
        }
    }

    private bool IsLinkedToConsole(
        EntityUid uid,
        EntityUid? approvingConsole
    )
    {
        if (approvingConsole is null
            || !TryComp<DeviceLinkSinkComponent>(uid, out var sinkComponent))
            return false;

        return sinkComponent.LinkedSources.Any(ent => ent == approvingConsole.Value);
    }

    private void SetEnabled(Entity<CargoTelepadComponent> ent, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(ent.Owner, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Turn off if disabled
        // Only change to Idle if off
        // don't overwrite teleporting state
        if (disabled)
            ent.Comp.CurrentState = CargoTelepadState.Unpowered;
        else if (ent.Comp.CurrentState == CargoTelepadState.Unpowered)
            ent.Comp.CurrentState = CargoTelepadState.Idle;

        _appearance.SetData(ent.Owner, CargoTelepadVisuals.State, ent.Comp.CurrentState);
    }
}
