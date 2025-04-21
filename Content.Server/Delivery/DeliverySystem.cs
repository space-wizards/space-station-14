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
    /// Name to use if the <see cref="DeliveryComponent.PenaltyBankAccount"/> is not set
    /// </summary>
    private const string UnknownAccount = "Unknown Account";

    /// <summary>
    /// Default reason to use if the penalization is triggered
    /// </summary>
    private const string DefaultMessage = "Warning";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);

        InitializeSpawning();
    }

    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.Container);

        if (_station.GetStationInMap(Transform(ent).MapID) is not EntityUid stationId)
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

        var delivery = ent.Comp;

        if (!TryComp<StationBankAccountComponent>(delivery.RecipientStation, out var account))
            return;

        var stationAccountEnt = (delivery.RecipientStation.Value, account);

        var ev = new GetDeliveryMultiplierEvent();
        RaiseLocalEvent(ent, ref ev);

        var multiplier = ev.Multiplier += ent.Comp.BaseSpesoMultiplier;

        _cargo.UpdateBankAccount(
            stationAccountEnt,
            (int)(delivery.BaseSpesoReward * multiplier),
           _cargo.CreateAccountDistribution((delivery.RecipientStation.Value, account)));
    }

    /// <summary>
    /// Allows the delivery to speak with the message and handles the charging of the penalized account
    /// </summary>
    /// <param name="ent"><see cref="DeliveryComponent"/> entity.</param>
    /// <param name="stationAccountEnt"><see cref="StationBankAccountComponent"/> entity.</param>
    public void HandlePenalty(Entity<DeliveryComponent> ent, Entity<StationBankAccountComponent?> stationAccountEnt, string reasonLoc)
    {
        var ev = new GetDeliveryMultiplierEvent();
        RaiseLocalEvent(ent, ref ev);

        var multiplier = ev.Multiplier += ent.Comp.BaseSpesoMultiplier;

        var delivery = ent.Comp;

        var accountName = (_protoMan.TryIndex(delivery.PenaltyBankAccount, out var accountInfo) &&
                   Loc.TryGetString(accountInfo.Name, out var localizedAccountName))
                  ? localizedAccountName
                  : UnknownAccount;

        if (!Loc.TryGetString(reasonLoc, out var reason))
            reason = DefaultMessage;

        _chat.TrySendInGameICMessage(ent, GetMessage(reason, delivery.BaseSpesoPenalty, accountName), InGameICChatType.Speak, hideChat: true);

        var dist = new Dictionary<ProtoId<CargoAccountPrototype>, double>()
        {
            { delivery.PenaltyBankAccount, 1.0 }
        };

        _cargo.UpdateBankAccount(
            stationAccountEnt,
            -((int)(delivery.BaseSpesoPenalty * multiplier)), // Subtracting
            dist
            );

        ent.Comp.WasPenalized = true;
    }

    private string GetMessage(string reason, int penalty, string accountName)
    {
        return Loc.GetString("delivery-penalty-message", ("reason", reason), ("spesos", penalty), ("account", accountName));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpawner(frameTime);
    }
}

[ByRefEvent]
public record struct GetDeliveryMultiplierEvent(float Multiplier = 0.0f);
