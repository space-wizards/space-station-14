using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.StationRecords;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    /// <summary>
    /// Default reason to use if the penalization is triggered
    /// </summary>
    private static readonly LocId DefaultMessage = "delivery-penalty-default-reason";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);

        InitializeSpawning();
    }

    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.Container);

        if (_station.GetStationInMap(Transform(ent).MapID) is not { } stationId)
            return;

        if (!_records.TryGetRandomRecord<GeneralStationRecord>(stationId, out var entry))
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJobTitle = entry.JobTitle;
        ent.Comp.RecipientStation = stationId;

        _appearance.SetData(ent, DeliveryVisuals.JobIcon, entry.JobIcon);

        _label.Label(ent, ent.Comp.RecipientName);

        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && entry.Fingerprint != null)
        {
            _fingerprintReader.AddAllowedFingerprint((ent.Owner, reader), entry.Fingerprint);
        }

        Dirty(ent);
    }

    protected override void GrantSpesoReward(Entity<DeliveryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<StationBankAccountComponent>(ent.Comp.RecipientStation, out var account))
            return;

        var stationAccountEnt = (ent.Comp.RecipientStation.Value, account);

        var multiplier = GetDeliveryMultiplier(ent!); // Resolve so we know it's got the component

        _cargo.UpdateBankAccount(
            stationAccountEnt,
            (int)(ent.Comp.BaseSpesoReward * multiplier),
           _cargo.CreateAccountDistribution((ent.Comp.RecipientStation.Value, account)));
    }

    /// <summary>
    /// Runs the penalty logic: Announcing the penalty and calculating how much to charge the designated account
    /// </summary>
    /// <param name="ent">The delivery for which to run the penalty.</param>
    /// <param name="reason">The penalty reason, displayed in front of the message.</param>
    protected override void HandlePenalty(Entity<DeliveryComponent> ent, string? reason = null)
    {
        if (!TryComp<StationBankAccountComponent>(ent.Comp.RecipientStation, out var stationAccount))
            return;

        if (ent.Comp.WasPenalized)
            return;

        if (!_protoMan.TryIndex(ent.Comp.PenaltyBankAccount, out var accountInfo))
            return;

        var multiplier = GetDeliveryMultiplier(ent);

        var localizedAccountName = Loc.GetString(accountInfo.Name);

        reason ??= Loc.GetString(DefaultMessage);

        var dist = new Dictionary<ProtoId<CargoAccountPrototype>, double>()
        {
            { ent.Comp.PenaltyBankAccount, 1.0 }
        };

        var penaltyAccountBalance = stationAccount.Accounts[ent.Comp.PenaltyBankAccount];
        var calculatedPenalty = (int)(ent.Comp.BaseSpesoPenalty * multiplier);

        // Prevents cargo from going into negatives
        if (calculatedPenalty > penaltyAccountBalance )
            calculatedPenalty = Math.Max(0, penaltyAccountBalance);

        _cargo.UpdateBankAccount(
            (ent.Comp.RecipientStation.Value, stationAccount),
            -calculatedPenalty,
            dist);

        var message = Loc.GetString("delivery-penalty-message", ("reason", reason), ("spesos", calculatedPenalty), ("account", localizedAccountName.ToUpper()));
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, hideChat: true);

        ent.Comp.WasPenalized = true;
        DirtyField(ent.Owner, ent.Comp, nameof(DeliveryComponent.WasPenalized));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpawner(frameTime);
    }
}
