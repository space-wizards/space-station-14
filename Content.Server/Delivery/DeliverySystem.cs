using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StationRecords;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Delivery;

/// <summary>
/// System for managing deliveries spawned by the mail teleporter.
/// This covers for letters/packages, as well as granting cargo money and spawning a reward for the player upon opening.
/// </summary>
public sealed partial class DeliverySystem : SharedDeliverySystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private readonly SharedLabelSystem _label = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeliveryComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DeliveryComponent, ActivateInWorldEvent>(OnUseInWorld);

        InitializeSpawning();
    }

    private void OnUseInHand(Entity<DeliveryComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (ent.Comp.IsOpened)
            return;

        if (ent.Comp.IsLocked)
            TryUnlockDelivery(ent, args.User);
        else
            OpenDelivery(ent, args.User);
    }

    private void OnUseInWorld(Entity<DeliveryComponent> ent, ref ActivateInWorldEvent args)
    {
        args.Handled = true;

        if (ent.Comp.IsOpened)
            return;

        if (ent.Comp.IsLocked)
            TryUnlockDelivery(ent, args.User);
        else
            OpenDelivery(ent, args.User, false);
    }

    private bool TryUnlockDelivery(Entity<DeliveryComponent> ent, EntityUid user, bool rewardMoney = true)
    {
        // Check fingerprint access if there is a reader on the mail
        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && !_fingerprintReader.IsAllowed((ent, reader), user))
            return false;

        var deliveryName = _nameModifier.GetBaseName(ent.Owner);

        _audio.PlayEntity(ent.Comp.UnlockSound, user, user);
        ent.Comp.IsLocked = false;
        UpdateAntiTamperVisuals(ent, false);

        var ev = new DeliveryUnlockedEvent(user);
        RaiseLocalEvent(ent, ref ev);

        if (rewardMoney)
            GrantSpesoReward(ent.AsNullable());

        _popup.PopupEntity(Loc.GetString("delivery-unlocked", ("delivery", deliveryName)), user, user);
        Dirty(ent);
        return true;
    }

    private void OpenDelivery(Entity<DeliveryComponent> ent, EntityUid user, bool attemptPickup = true)
    {
        var deliveryName = _nameModifier.GetBaseName(ent.Owner);

        _audio.PlayEntity(ent.Comp.OpenSound, user, user);

        var ev = new DeliveryOpenedEvent(user);
        RaiseLocalEvent(ent, ref ev);

        _hands.TryDrop(user);

        ent.Comp.IsOpened = true;
        _appearance.SetData(ent, DeliveryVisuals.IsTrash, ent.Comp.IsOpened);

        _tag.AddTags(ent, "Trash", "Recyclable");
        EnsureComp<SpaceGarbageComponent>(ent);

        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("delivery-opened", ("delivery", deliveryName)), user, user);

        if (!_container.TryGetContainer(ent, "delivery", out var container))
            return;

        if (attemptPickup)
        {
            foreach (var entity in container.ContainedEntities.ToArray())
            {
                _hands.PickupOrDrop(user, entity);
            }
        }
        else
        {
            _container.EmptyContainer(container, true, Transform(ent.Owner).Coordinates);
        }
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
        var container = _container.EnsureContainer<Container>(ent, "delivery");

        var stationId = _station.GetStationInMap(Transform(ent).MapID);

        if (stationId == null)
            return;

        _records.TryGetRandomRecord<GeneralStationRecord>(stationId.Value, out var entry);

        if (entry == null)
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJobTitle = entry.JobTitle;
        ent.Comp.RecipientStation = stationId.Value;
        ent.Comp.Container = container;

        _appearance.SetData(ent, DeliveryVisuals.JobIcon, entry.JobIcon);

        _label.Label(ent, ent.Comp.RecipientName);

        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && entry.Fingerprint != null)
        {
            _fingerprintReader.AddAllowedFingerprint((ent.Owner, reader), entry.Fingerprint);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpawner(frameTime);
    }
}

/// <summary>
/// Event raised on the delivery when it is unlocked.
/// </summary>
[ByRefEvent]
public readonly record struct DeliveryUnlockedEvent(EntityUid User);

/// <summary>
/// Event raised on the delivery when it is opened.
/// </summary>
[ByRefEvent]
public readonly record struct DeliveryOpenedEvent(EntityUid User);
