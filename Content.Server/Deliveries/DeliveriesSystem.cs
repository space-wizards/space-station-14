using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Deliveries;
using Content.Shared.FingerprintReader;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.StationRecords;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Deliveries;

/// <summary>
/// If you're reading this you're gay but server side
/// </summary>
public sealed class DeliveriesSystem : SharedDeliveriesSystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeliveryComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SpawnTableOnUseSystem)]);
    }

    private void OnUseInHand(Entity<DeliveryComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.IsLocked)
        {
            // Always handle the event during unlock attempts to prevent SpawnTableOnUse
            args.Handled = true;

            if (TryUnlockDelivery(ent, args.User))
            {
                // Successfully unlocked, but don't open yet
                return;
            }
        }
        else
        {
            // Don't handle the event - let SpawnTableOnUse fire
            OpenDelivery(ent, args.User);
        }
    }

    private bool TryUnlockDelivery(Entity<DeliveryComponent> ent, EntityUid user, bool rewardMoney = true)
    {
        // Check fingerprint access if there is a reader on the mail
        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && !_fingerprintReader.IsAllowed((ent, reader), user))
            return false;

        // TODO: Separate this from the OpenSound into UnlockSound
        _audio.PlayEntity(ent.Comp.OpenSound, user, user);
        ent.Comp.IsLocked = false;
        UpdateAntiTamperVisuals(ent, false);

        if (rewardMoney)
            GrantSpesoReward(ent.AsNullable());

        _popup.PopupEntity(Loc.GetString("delivery-unlocked"), user, user);
        Dirty(ent);
        return true;
    }

    private void OpenDelivery(Entity<DeliveryComponent> ent, EntityUid user)
    {
        _audio.PlayEntity(ent.Comp.OpenSound, user, user);

        if (ent.Comp.Wrapper != null)
            Spawn(ent.Comp.Wrapper, Transform(user).Coordinates);

        _popup.PopupEntity(Loc.GetString("delivery-opened"), user, user);
    }

    // TODO: generic updateVisuals from component data
    private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
    {
        _appearance.SetData(uid, DeliveryVisuals.IsLocked, isLocked);

        // If we're trying to unlock, always remove the priority tape
        if (!isLocked)
            _appearance.SetData(uid, DeliveryVisuals.IsPriority, false);
    }


    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
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

        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && entry.Fingerprint != null)
        {
            reader.AllowedFingerprints.Add(entry.Fingerprint);
            Dirty(ent, reader);
        }

        Dirty(ent);
    }

    private void GrantSpesoReward(Entity<DeliveryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<StationBankAccountComponent>(ent.Comp.RecipientStation, out var account))
            return;

        _cargo.UpdateBankAccount(ent, account, ent.Comp.SpesoReward);
    }
}
