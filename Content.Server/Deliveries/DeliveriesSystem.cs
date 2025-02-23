using System.Diagnostics;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Biocoded;
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

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<TransformComponent>(ent, out var transform))
            return;

        var stationId = _station.GetStationInMap(transform.MapID);

        if (stationId == null)
            return;

        _records.TryGetRandomRecord<GeneralStationRecord>(stationId.Value, out var entry);

        if (entry == null)
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJob = entry.JobTitle;
        ent.Comp.RecipientStation = stationId.Value;

        Dirty(ent);

        if (TryComp<BiocodedComponent>(ent, out var biocoded))
        {
            biocoded.Fingerprint = entry.Fingerprint;
            Dirty(ent, biocoded);
        }
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
