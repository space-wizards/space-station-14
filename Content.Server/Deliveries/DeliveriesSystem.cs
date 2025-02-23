using System.Diagnostics;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Deliveries;
using Content.Shared.Ghost;
using Content.Shared.StationRecords;
using Robust.Shared.Timing;

namespace Content.Server.Deliveries;

/// <summary>
/// If you're reading this you're gay but server side
/// </summary>
public sealed class DeliveriesSystem : SharedDeliveriesSystem
{

    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    public override void Initialize()
    {
        base.Initialize();

    }

    protected override void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);

        if (!TryComp<TransformComponent>(ent, out var transform))
            return;

        var stationId = _station.GetStationInMap(transform.MapID);

        Log.Debug("Getting station");

        if (stationId == null)
            return;

        _records.TryGetRandomRecord<GeneralStationRecord>(stationId.Value, out var entry);

        Log.Debug("Getting record");

        if (entry == null)
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJob = entry.JobTitle;
        ent.Comp.RecipientStation = stationId.Value;
        if(entry.Fingerprint != null)
            ent.Comp.RecipientFingerprint = entry.Fingerprint;

        Dirty(ent);
    }

    protected override void GrantSpesoReward(EntityUid uid, DeliveryComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<StationBankAccountComponent>(comp.RecipientStation, out var account))
            return;

        _cargo.UpdateBankAccount(uid, account, comp.SpesoReward);

    }
}
