using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.StationRecords;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Delivery;

/// <summary>
/// System for managing deliveries spawned by the mail teleporter.
/// This covers for mail spawning, as well as granting cargo money.
/// </summary>
public sealed partial class DeliverySystem : SharedDeliverySystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private readonly SharedLabelSystem _label = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);

        InitializeSpawning();
    }

    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.Container);

        var stationId = _station.GetStationInMap(Transform(ent).MapID);

        if (stationId == null)
            return;

        _records.TryGetRandomRecord<GeneralStationRecord>(stationId.Value, out var entry);

        if (entry == null)
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJobTitle = entry.JobTitle;
        ent.Comp.RecipientStation = stationId.Value;

        _appearance.SetData(ent, DeliveryVisuals.JobIcon, entry.JobIcon);

        _label.Label(ent, ent.Comp.RecipientName);

        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && entry.Fingerprint != null)
        {
            _fingerprintReader.AddAllowedFingerprint((ent.Owner, reader), entry.Fingerprint);
        }

        Dirty(ent);
    }

    protected override void ModifySpesoAmount(Entity<DeliveryComponent?> ent, int amount = 0)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<StationBankAccountComponent>(ent.Comp.RecipientStation, out var account))
            return;

        var spesoAmount = amount != 0 ? amount : ent.Comp.SpesoReward;

        // Do you want to bring cargo into debt???
        if (spesoAmount < 0 && account.Balance + spesoAmount < 0)
            spesoAmount = account.Balance * -1;

        _cargo.UpdateBankAccount(ent.Comp.RecipientStation.Value, account, spesoAmount);
    }

    protected override void AnnounceTearPenalty(Entity<TearableDeliveryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.TearMessage == null)
            return;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.TearMessage, ("spesos", ent.Comp.SpesoPenalty)), InGameICChatType.Speak, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpawner(frameTime);
    }
}
