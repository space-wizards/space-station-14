using System.Linq;
using Content.Server._FTL.ShipHealth;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.UserInterface;
using Content.Shared._FTL.Weapons;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._FTL.Weapons;

/// <inheritdoc/>
public sealed class WeaponTargetingSystem : SharedWeaponTargetingSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ShipTrackerSystem _shipTrackerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponTargetingUserComponent, EntParentChangedMessage>(OnUserParentChanged);
        SubscribeLocalEvent<WeaponTargetingComponent, BoundUIOpenedEvent>(OnStationMapOpened);
        SubscribeLocalEvent<WeaponTargetingComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<WeaponTargetingComponent, BoundUIClosedEvent>(OnStationMapClosed);
        SubscribeLocalEvent<WeaponTargetingComponent, FireWeaponSendMessage>(OnFireWeaponSendMessage);
        SubscribeLocalEvent<WeaponTargetingComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<WeaponTargetingComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<WeaponTargetingComponent, ShipScanRequestMessage>(OnShipScanRequest);

        SubscribeLocalEvent<FTLWeaponComponent, SignalReceivedEvent>(WeaponSignalReceived);

        SubscribeLocalEvent<FTLWeaponSiloComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<FTLWeaponSiloComponent, StorageAfterOpenEvent>(OnOpen);
    }

    public override void Update(float delta)
    {
        var query = EntityQueryEnumerator<FTLActiveCooldownWeaponComponent>();
        while (query.MoveNext(out var entityUid, out var comp))
        {
            comp.SecondsLeft -= delta;
            if (comp.SecondsLeft <= 0)
            {
                TryComp<FTLWeaponComponent>(entityUid, out var weaponComponent);
                TryComp<WeaponTargetingComponent>(entityUid, out var targetingComponent);
                if (weaponComponent != null)
                    weaponComponent.CanBeUsed = true;
                if (targetingComponent != null)
                {
                    targetingComponent.CanFire = true;
                    UpdateState(entityUid, targetingComponent.CanFire);
                }

                _entityManager.RemoveComponent<FTLActiveCooldownWeaponComponent>(entityUid);
            }
        }
    }

    #region Linking

    private void OnPortDisconnected(EntityUid uid, WeaponTargetingComponent component, PortDisconnectedEvent args)
    {
        Log.Debug("no link");
        component.IsLinked = false;
    }

    private void OnNewLink(EntityUid uid, WeaponTargetingComponent component, NewLinkEvent args)
    {
        Log.Debug("new link");
        component.IsLinked = true;
    }

    #endregion

    #region Signals
    private void WeaponSignalReceived(EntityUid uid, FTLWeaponComponent component, ref SignalReceivedEvent args)
    {
        if (!component.CanBeUsed)
        {
            _audioSystem.PlayPvs(component.CooldownSound, uid);
            return;
        }

        if (args.Data == null)
            return;

        if (
            args.Data["coordinates"] is not EntityCoordinates &&
            args.Data["targetGrid"] is not EntityUid &&
            args.Data["weaponPad"] is not EntityUid
        )
        {
            return;
        }

        var coordinates = (EntityCoordinates) args.Data["coordinates"];
        var targetGrid = (EntityUid) args.Data["targetGrid"];
        var weaponPad = (EntityUid) args.Data["weaponPad"];

        TryFireWeapon(uid, component, targetGrid, coordinates, weaponPad);
    }

    public void TryFireWeapon(EntityUid uid, FTLWeaponComponent component, EntityUid targetGrid, EntityCoordinates coordinates, EntityUid? weaponPad)
    {
        TryComp<FTLWeaponSiloComponent>(uid, out var siloComponent);
        string? ammoPrototypeString = null;

        if (siloComponent != null && siloComponent.ContainedEntities != null)
        {
            foreach (var entity in siloComponent.ContainedEntities)
            {
                if (ammoPrototypeString == null)
                {
                    if (siloComponent.AmmoWhitelist != null && !siloComponent.AmmoWhitelist.IsValid(entity))
                    {
                        if (!TryComp<TransformComponent>(uid, out var transform))
                            return;
                        _popupSystem.PopupCoordinates(Loc.GetString("weapon-popup-incorrect-ammo-message"), transform.Coordinates);
                        _entityManager.DeleteEntity(entity);
                        continue;
                    }
                    ammoPrototypeString = component.Prototype;
                }

                _entityManager.DeleteEntity(entity);
            }
        }
        else
        {
            ammoPrototypeString = component.Prototype;
        }

        var localeMessage = "weapon-pad-message-miss-text";
        if (ammoPrototypeString == null)
        {
            _audioSystem.PlayPvs(component.CooldownSound, uid);
            return;
        }
        var ammoPrototype = _prototypeManager.Index<FTLAmmoType>(ammoPrototypeString);
        TryComp<ShipTrackerComponent>(targetGrid, out var shipHealthComponent);
        if (shipHealthComponent != null)
        {
            if (_shipTrackerSystem.TryDamageShip(shipHealthComponent, ammoPrototype))
            {
                _entityManager.SpawnEntity(ammoPrototype.BulletPrototype, coordinates);
                localeMessage = "weapon-pad-message-hit-text";
            }

            if (weaponPad.HasValue)
                _popupSystem.PopupEntity(Loc.GetString(localeMessage, ("hull", shipHealthComponent.HullAmount), ("maxHull", shipHealthComponent.HullCapacity), ("shields", shipHealthComponent.ShieldAmount)), weaponPad.Value);
        }
        _audioSystem.PlayPvs(component.FireSound, uid);

        component.CanBeUsed = false;
        TryCooldownWeapon(uid, component);
    }

    private void TryCooldownWeapon(EntityUid uid, FTLWeaponComponent component)
    {
        var comp = EnsureComp<FTLActiveCooldownWeaponComponent>(uid);
        comp.SecondsLeft = component.CooldownTime;
    }

    private void TryCooldownWeapon(EntityUid uid, WeaponTargetingComponent component)
    {
        var comp = EnsureComp<FTLActiveCooldownWeaponComponent>(uid);
        comp.SecondsLeft = component.CooldownTime;
    }

    private void OnFireWeaponSendMessage(EntityUid uid, WeaponTargetingComponent component, FireWeaponSendMessage args)
    {
        if (!Equals(args.UiKey, WeaponTargetingUiKey.Key) || args.Session.AttachedEntity == null)
            return;

        if (TryComp<FTLActiveCooldownWeaponComponent>(uid, out var activeComp))
        {
            _audioSystem.PlayPvs(component.CooldownSound, uid);
            return;
        }

        var payload = new NetworkPayload
        {
            ["message"] = "goofball",
            ["coordinates"] = args.Coordinates,
            ["targetGrid"] = args.TargetGrid,
            ["weaponPad"] = uid,
        };

        _deviceLinkSystem.InvokePort(uid, "WeaponOutputPort", payload);
        TryCooldownWeapon(uid, component);
    }
    #endregion

    #region UI
    private void OnClose(EntityUid uid, FTLWeaponSiloComponent component, ref StorageAfterCloseEvent args)
    {
        TryComp<EntityStorageComponent>(uid, out var container);
        if (container == null)
            return;
        component.ContainedEntities = new List<EntityUid>();
        foreach (var entity in container.Contents.ContainedEntities)
        {
            component.ContainedEntities.Add(entity);
        }
    }

    private void OnOpen(EntityUid uid, FTLWeaponSiloComponent component, ref StorageAfterOpenEvent args)
    {
        if (component.ContainedEntities == null)
            return;

        if (!TryComp<TransformComponent>(uid, out var transform))
            return;

        foreach (var entity in component.ContainedEntities)
        {
            _physicsSystem.ApplyLinearImpulse(entity, -(transform.LocalRotation.ToWorldVec() * 100f));
            var damage = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"),
                FixedPoint2.New(25));
            _damageableSystem.TryChangeDamage(entity, damage);
            _staminaSystem.TakeStaminaDamage(entity, 100f);
        }
    }

    private void OnShipScanRequest(EntityUid uid, WeaponTargetingComponent component, ShipScanRequestMessage args)
    {
        TryComp<ShipTrackerComponent>(args.SelectedGrid, out var shipTrackerComponent);
        if (shipTrackerComponent == null)
            return;

        var message = Loc.GetString("weapon-pad-message-scan-text",
            ("hull", shipTrackerComponent.HullAmount),
            ("maxHull", shipTrackerComponent.HullCapacity),
            ("shields", shipTrackerComponent.ShieldAmount),
            ("maxShields", shipTrackerComponent.ShieldCapacity)
        );
        UpdateState(uid, component.CanFire, message);
    }

    private void OnUIOpenAttempt(EntityUid uid, WeaponTargetingComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!component.IsLinked)
        {
            args.Cancel();
            if (!TryComp<TransformComponent>(uid, out var transform))
                return;
            _popupSystem.PopupCoordinates(Loc.GetString("weapon-popup-no-link-message"), transform.Coordinates);
        }
    }


    private void OnStationMapClosed(EntityUid uid, WeaponTargetingComponent component, BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, WeaponTargetingUiKey.Key) || args.Session.AttachedEntity == null)
            return;

        RemCompDeferred<WeaponTargetingUserComponent>(args.Session.AttachedEntity.Value);
    }

    private void OnUserParentChanged(EntityUid uid, WeaponTargetingUserComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp<ActorComponent>(uid, out var actor))
        {
            _uiSystem.TryClose(component.Map, WeaponTargetingUiKey.Key, actor.PlayerSession);
        }
    }

    private void OnStationMapOpened(EntityUid uid, WeaponTargetingComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        var comp = EnsureComp<WeaponTargetingUserComponent>(args.Session.AttachedEntity.Value);

        UpdateState(uid, component.CanFire);
        comp.Map = uid;
    }

    private void UpdateState(EntityUid uid, bool canFire, string? scanText = null)
    {
        // collect grids
        var grids = EntityQuery<ShipTrackerComponent>().Select(x => x.Owner).ToList();
        var state = new WeaponTargetingUserInterfaceState(canFire, grids, scanText is null ? "" : scanText);
        _uiSystem.TrySetUiState(uid, WeaponTargetingUiKey.Key, state);
    }
    #endregion
}
