using System;
using System.Collections.Generic;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.PowerCell;
using Content.Server.Stunnable;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionBlockerSystem  _blocker = default!;
    [Dependency] private readonly AdminLogSystem _logs = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly CameraRecoilSystem _recoil = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EffectSystem _effects = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    /// <summary>
    /// How many sounds are allowed to be played on ejecting multiple casings.
    /// </summary>
    private const int EjectionSoundMax = 3;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: So at the time I thought there might've been a need to keep magazines
        // and ammo boxes separate.
        // There isn't.
        // They should be combined.

        SubscribeLocalEvent<AmmoComponent, ExaminedEvent>(OnAmmoExamine);

        SubscribeLocalEvent<AmmoBoxComponent, ComponentInit>(OnAmmoBoxInit);
        SubscribeLocalEvent<AmmoBoxComponent, MapInitEvent>(OnAmmoBoxMapInit);
        SubscribeLocalEvent<AmmoBoxComponent, ExaminedEvent>(OnAmmoBoxExamine);
        SubscribeLocalEvent<AmmoBoxComponent, InteractUsingEvent>(OnAmmoBoxInteractUsing);
        SubscribeLocalEvent<AmmoBoxComponent, UseInHandEvent>(OnAmmoBoxUse);
        SubscribeLocalEvent<AmmoBoxComponent, InteractHandEvent>(OnAmmoBoxInteractHand);
        SubscribeLocalEvent<AmmoBoxComponent, GetVerbsEvent<AlternativeVerb>>(OnAmmoBoxAltVerbs);

        SubscribeLocalEvent<RangedMagazineComponent, ComponentInit>(OnRangedMagInit);
        SubscribeLocalEvent<RangedMagazineComponent, MapInitEvent>(OnRangedMagMapInit);
        SubscribeLocalEvent<RangedMagazineComponent, UseInHandEvent>(OnRangedMagUse);
        SubscribeLocalEvent<RangedMagazineComponent, ExaminedEvent>(OnRangedMagExamine);
        SubscribeLocalEvent<RangedMagazineComponent, InteractUsingEvent>(OnRangedMagInteractUsing);

        // Whenever I get around to refactoring guns this is all going to change.
        // Essentially the idea is
        // You have GunComponent and ChamberedGunComponent (which is just guncomp + containerslot for chamber)
        // GunComponent has a component for an ammo provider on it (e.g. battery) and asks it for ammo to shoot
        // ALTERNATIVELY, it has a "MagazineAmmoProvider" that has its own containerslot that it can ask
        // (All of these would be comp references so max you only ever have 2 components on the gun).
        SubscribeLocalEvent<BatteryBarrelComponent, ComponentInit>(OnBatteryInit);
        SubscribeLocalEvent<BatteryBarrelComponent, MapInitEvent>(OnBatteryMapInit);
        SubscribeLocalEvent<BatteryBarrelComponent, ComponentGetState>(OnBatteryGetState);
        SubscribeLocalEvent<BatteryBarrelComponent, PowerCellChangedEvent>(OnCellSlotUpdated);

        SubscribeLocalEvent<BoltActionBarrelComponent, ComponentInit>(OnBoltInit);
        SubscribeLocalEvent<BoltActionBarrelComponent, MapInitEvent>(OnBoltMapInit);
        SubscribeLocalEvent<BoltActionBarrelComponent, GunFireAttemptEvent>(OnBoltFireAttempt);
        SubscribeLocalEvent<BoltActionBarrelComponent, UseInHandEvent>(OnBoltUse);
        SubscribeLocalEvent<BoltActionBarrelComponent, InteractUsingEvent>(OnBoltInteractUsing);
        SubscribeLocalEvent<BoltActionBarrelComponent, ComponentGetState>(OnBoltGetState);
        SubscribeLocalEvent<BoltActionBarrelComponent, ExaminedEvent>(OnBoltExamine);
        SubscribeLocalEvent<BoltActionBarrelComponent, GetVerbsEvent<InteractionVerb>>(AddToggleBoltVerb);

        SubscribeLocalEvent<MagazineBarrelComponent, ComponentInit>(OnMagazineInit);
        SubscribeLocalEvent<MagazineBarrelComponent, MapInitEvent>(OnMagazineMapInit);
        SubscribeLocalEvent<MagazineBarrelComponent, ExaminedEvent>(OnMagazineExamine);
        SubscribeLocalEvent<MagazineBarrelComponent, UseInHandEvent>(OnMagazineUse);
        SubscribeLocalEvent<MagazineBarrelComponent, InteractUsingEvent>(OnMagazineInteractUsing);
        SubscribeLocalEvent<MagazineBarrelComponent, ComponentGetState>(OnMagazineGetState);
        SubscribeLocalEvent<MagazineBarrelComponent, GetVerbsEvent<InteractionVerb>>(AddMagazineInteractionVerbs);
        SubscribeLocalEvent<MagazineBarrelComponent, GetVerbsEvent<AlternativeVerb>>(AddEjectMagazineVerb);

        SubscribeLocalEvent<PumpBarrelComponent, ComponentGetState>(OnPumpGetState);
        SubscribeLocalEvent<PumpBarrelComponent, ComponentInit>(OnPumpInit);
        SubscribeLocalEvent<PumpBarrelComponent, MapInitEvent>(OnPumpMapInit);
        SubscribeLocalEvent<PumpBarrelComponent, ExaminedEvent>(OnPumpExamine);
        SubscribeLocalEvent<PumpBarrelComponent, UseInHandEvent>(OnPumpUse);
        SubscribeLocalEvent<PumpBarrelComponent, InteractUsingEvent>(OnPumpInteractUsing);

        SubscribeLocalEvent<RevolverBarrelComponent, MapInitEvent>(OnRevolverMapInit);
        SubscribeLocalEvent<RevolverBarrelComponent, UseInHandEvent>(OnRevolverUse);
        SubscribeLocalEvent<RevolverBarrelComponent, InteractUsingEvent>(OnRevolverInteractUsing);
        SubscribeLocalEvent<RevolverBarrelComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<RevolverBarrelComponent, GetVerbsEvent<AlternativeVerb>>(AddSpinVerb);

        SubscribeLocalEvent<SpeedLoaderComponent, ComponentInit>(OnSpeedLoaderInit);
        SubscribeLocalEvent<SpeedLoaderComponent, MapInitEvent>(OnSpeedLoaderMapInit);
        SubscribeLocalEvent<SpeedLoaderComponent, UseInHandEvent>(OnSpeedLoaderUse);
        SubscribeLocalEvent<SpeedLoaderComponent, AfterInteractEvent>(OnSpeedLoaderAfterInteract);
        SubscribeLocalEvent<SpeedLoaderComponent, InteractUsingEvent>(OnSpeedLoaderInteractUsing);

        // SubscribeLocalEvent<ServerRangedWeaponComponent, ExaminedEvent>(OnGunExamine);
        SubscribeNetworkEvent<FirePosEvent>(OnFirePos);
    }

    private void OnFirePos(FirePosEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {Valid: true} user)
            return;

        if (!msg.Coordinates.IsValid(EntityManager))
            return;

        if (!TryComp(user, out HandsComponent? handsComponent))
            return;

        // TODO: Not exactly robust
        var gun = handsComponent.GetActiveHand()?.HeldEntity;

        if (gun == null || !TryComp(gun, out ServerRangedWeaponComponent? weapon))
            return;

        // map pos
        TryFire(user, msg.Coordinates, weapon);
    }

    public EntityUid? PeekAtAmmo(ServerRangedBarrelComponent component)
    {
        return component switch
        {
            BatteryBarrelComponent battery => PeekAmmo(battery),
            BoltActionBarrelComponent bolt => PeekAmmo(bolt),
            MagazineBarrelComponent mag => PeekAmmo(mag),
            PumpBarrelComponent pump => PeekAmmo(pump),
            RevolverBarrelComponent revolver => PeekAmmo(revolver),
            _ => throw new NotImplementedException()
        };
    }

    public EntityUid? TakeOutProjectile(ServerRangedBarrelComponent component, EntityCoordinates spawnAt)
    {
        return component switch
        {
            BatteryBarrelComponent battery => TakeProjectile(battery, spawnAt),
            BoltActionBarrelComponent bolt => TakeProjectile(bolt, spawnAt),
            MagazineBarrelComponent mag => TakeProjectile(mag, spawnAt),
            PumpBarrelComponent pump => TakeProjectile(pump, spawnAt),
            RevolverBarrelComponent revolver => TakeProjectile(revolver, spawnAt),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Drops multiple cartridges / shells on the floor
    /// Wraps EjectCasing to make it less toxic for bulk ejections
    /// </summary>
    public void EjectCasings(IEnumerable<EntityUid> entities)
    {
        var soundPlayCount = 0;
        var playSound = true;

        foreach (var entity in entities)
        {
            EjectCasing(entity, playSound);
            soundPlayCount++;
            if (soundPlayCount > EjectionSoundMax)
            {
                playSound = false;
            }
        }
    }

    /// <summary>
    /// Drops a single cartridge / shell
    /// </summary>
    public void EjectCasing(
        EntityUid entity,
        bool playSound = true,
        AmmoComponent? ammoComponent = null)
    {
        const float ejectOffset = 0.4f;

        if (!Resolve(entity, ref ammoComponent)) return;

        var offsetPos = (_random.NextFloat(-ejectOffset, ejectOffset), _random.NextFloat(-ejectOffset, ejectOffset));

        var xform = Transform(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        xform.LocalRotation = _random.NextFloat(MathF.Tau);
        xform.Coordinates = coordinates;

        if (playSound)
            SoundSystem.Play(Filter.Pvs(entity), ammoComponent.SoundCollectionEject.GetSound(), coordinates, AudioParams.Default.WithVolume(-1));
    }

    private Angle GetRecoilAngle(ServerRangedBarrelComponent component, Angle direction)
    {
        var currentTime = _gameTiming.CurTime;
        var timeSinceLastFire = (currentTime - component.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(component.CurrentAngle.Theta + component.AngleIncrease - component.AngleDecay * timeSinceLastFire, component.MinAngle.Theta, component.MaxAngle.Theta);
        component.CurrentAngle = new Angle(newTheta);

        var random = (_random.NextDouble(-1, 1));
        var angle = Angle.FromDegrees(direction.Degrees + component.CurrentAngle.Degrees * random);
        return angle;
    }

    /// <summary>
    /// Raised on a gun when it fires.
    /// </summary>
    public sealed class GunShotEvent : EntityEventArgs
    {

    }

    public sealed class GunFireAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid? User = null;
        public ServerRangedWeaponComponent Weapon;

        public GunFireAttemptEvent(EntityUid? user, ServerRangedWeaponComponent weapon)
        {
            User = user;
            Weapon = weapon;
        }
    }
}
