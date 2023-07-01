using System.Linq;
using Content.Server._FTL.ShipHealth;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared._FTL.Weapons;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Server._FTL.Weapons;

/// <inheritdoc/>
public sealed class WeaponTargetingSystem : SharedWeaponTargetingSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly FTLShipHealthSystem _shipHealthSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponTargetingUserComponent, EntParentChangedMessage>(OnUserParentChanged);
        SubscribeLocalEvent<WeaponTargetingComponent, BoundUIOpenedEvent>(OnStationMapOpened);
        SubscribeLocalEvent<WeaponTargetingComponent, BoundUIClosedEvent>(OnStationMapClosed);
        SubscribeLocalEvent<WeaponTargetingComponent, FireWeaponSendMessage>(OnFireWeaponSendMessage);

        SubscribeLocalEvent<FTLWeaponComponent, SignalReceivedEvent>(WeaponSignalReceived);

        SubscribeLocalEvent<FTLWeaponSiloComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<FTLWeaponSiloComponent, StorageAfterOpenEvent>(OnOpen);
    }

    public override void Update(float delta)
    {
        foreach (var comp in EntityManager.EntityQuery<FTLActiveCooldownWeaponComponent>())
        {
            comp.SecondsLeft -= delta;
            if (comp.SecondsLeft <= 0)
            {
                TryComp<FTLWeaponComponent>(comp.Owner, out var weaponComponent);
                TryComp<WeaponTargetingComponent>(comp.Owner, out var targetingComponent);
                if (weaponComponent != null)
                    weaponComponent.CanBeUsed = true;
                if (targetingComponent != null)
                    targetingComponent.CanFire = true;

                _entityManager.RemoveComponent<FTLActiveCooldownWeaponComponent>(comp.Owner);
            }
        }
    }

    private void WeaponSignalReceived(EntityUid uid, FTLWeaponComponent component, ref SignalReceivedEvent args)
    {
        if (!component.CanBeUsed)
        {
            _audioSystem.PlayPvs(component.CooldownSound, uid);
            return;
        }

        if (args.Data == null)
            return;

        foreach(KeyValuePair<string, object> entry in args.Data)
        {
            Logger.Debug($"{entry.Key}: {entry.Value}");
        }

        var coordinates = (EntityCoordinates) args.Data["coordinates"];
        var targetGrid = (EntityUid) args.Data["targetGrid"];
        var weaponPad = (EntityUid) args.Data["weaponPad"];

        TryFireWeapon(uid, component, targetGrid, weaponPad, coordinates);
    }

    private void TryFireWeapon(EntityUid uid, FTLWeaponComponent component, EntityUid targetGrid, EntityUid weaponPad, EntityCoordinates coordinates)
    {
        TryComp<FTLWeaponSiloComponent>(uid, out var siloComponent);
        var ammoPrototypeString = "";

        if (siloComponent != null)
        {
            if (siloComponent.ContainedEntities != null)
            {
                foreach (var entity in siloComponent.ContainedEntities)
                {
                    TryComp<FTLAmmoComponent>(entity, out var ammoComponent);
                    if (ammoComponent != null)
                    {
                        Logger.Debug(ammoComponent.Prototype);
                        ammoPrototypeString = ammoComponent.Prototype;
                    }
                    _entityManager.DeleteEntity(entity);
                }
            }
        }
        else
        {
            ammoPrototypeString = component.Prototype;
        }

        var localeMessage = "weapon-pad-message-hit-text";
        if (ammoPrototypeString == "")
        {
            _audioSystem.PlayPvs(component.CooldownSound, uid);
            return;
        }
        var ammoPrototype = _prototypeManager.Index<FTLAmmoType>(ammoPrototypeString);
        TryComp<FTLShipHealthComponent>(targetGrid, out var shipHealthComponent);
        if (shipHealthComponent != null && _shipHealthSystem.TryDamageShip(shipHealthComponent, ammoPrototype))
        {
            _entityManager.SpawnEntity(ammoPrototype.Prototype, coordinates);
            localeMessage = "weapon-pad-message-miss-text";
        }
        _chatSystem.TrySendInGameICMessage(weaponPad, Loc.GetString(localeMessage), InGameICChatType.Speak, false);

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

        var transform = Transform(uid);
        foreach (var entity in component.ContainedEntities)
        {
            _physicsSystem.ApplyLinearImpulse(entity, -(transform.LocalRotation.ToWorldVec() * 100000f));
            var damage = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"),
                FixedPoint2.New(50));
            _damageableSystem.TryChangeDamage(entity, damage);
            _staminaSystem.TakeStaminaDamage(entity, 100f);
        }
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
        // collect grids
        var grids = EntityQuery<FTLShipHealthComponent>().Select(x => x.Owner).ToList();
        var state = new WeaponTargetingUserInterfaceState(component.CanFire, grids);
        foreach (var grid in grids)
        {
            Logger.Debug(grid.ToString());
        }
        _uiSystem.TrySetUiState(uid, WeaponTargetingUiKey.Key, state);
        comp.Map = uid;
    }
}
