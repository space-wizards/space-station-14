using System;
using System.Collections.Generic;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
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
    [Dependency] private readonly EffectSystem _effects = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// How many sounds are allowed to be played on ejecting multiple casings.
    /// </summary>
    private const int EjectionSoundMax = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmmoComponent, ExaminedEvent>(OnAmmoExamine);

        SubscribeLocalEvent<AmmoBoxComponent, ComponentInit>(OnAmmoBoxInit);
        SubscribeLocalEvent<AmmoBoxComponent, MapInitEvent>(OnAmmoBoxMapInit);
        SubscribeLocalEvent<AmmoBoxComponent, ExaminedEvent>(OnAmmoBoxExamine);

        SubscribeLocalEvent<AmmoBoxComponent, InteractUsingEvent>(OnAmmoBoxInteractUsing);
        SubscribeLocalEvent<AmmoBoxComponent, UseInHandEvent>(OnAmmoBoxUse);
        SubscribeLocalEvent<AmmoBoxComponent, InteractHandEvent>(OnAmmoBoxInteractHand);
        SubscribeLocalEvent<AmmoBoxComponent, GetAlternativeVerbsEvent>(OnAmmoBoxAltVerbs);

        // Whenever I get around to refactoring guns this is all going to change.
        SubscribeLocalEvent<BoltActionBarrelComponent, ComponentInit>(OnBoltInit);
        SubscribeLocalEvent<BoltActionBarrelComponent, MapInitEvent>(OnBoltMapInit);
        SubscribeLocalEvent<BoltActionBarrelComponent, GunFireAttemptEvent>(OnBoltFireAttempt);
        SubscribeLocalEvent<BoltActionBarrelComponent, UseInHandEvent>(OnBoltUse);
        SubscribeLocalEvent<BoltActionBarrelComponent, InteractUsingEvent>(OnBoltInteractUsing);
        SubscribeLocalEvent<BoltActionBarrelComponent, ComponentGetState>(OnBoltGetState);

        SubscribeLocalEvent<RevolverBarrelComponent, MapInitEvent>(OnRevolverMapInit);
        SubscribeLocalEvent<RevolverBarrelComponent, UseInHandEvent>(OnRevolverUse);
        SubscribeLocalEvent<RevolverBarrelComponent, InteractUsingEvent>(OnRevolverInteractUsing);
        SubscribeLocalEvent<RevolverBarrelComponent, ComponentGetState>(OnRevolverGetState);

        SubscribeLocalEvent<ServerRangedWeaponComponent, ExaminedEvent>(OnGunExamine);
        SubscribeLocalEvent<BoltActionBarrelComponent, ExaminedEvent>(OnBoltExamine);
        SubscribeLocalEvent<PumpBarrelComponent, ExaminedEvent>(OnPumpExamine);
        SubscribeLocalEvent<ServerMagazineBarrelComponent, ExaminedEvent>(OnMagazineExamine);
    }

    public EntityUid? PeekAmmo(ServerRangedBarrelComponent component)
    {
        switch (component)
        {
            default:
                throw new NotImplementedException();
        }
    }

    public EntityUid? TakeProjectile(ServerRangedBarrelComponent component, EntityCoordinates spawnAt)
    {
        switch (component)
        {
            default:
                throw new NotImplementedException();
        }
    }

    private void SetEjectDirections(ref Direction[]? directions)
    {
        if (directions != null) return;

        directions = new[]
        {
            Direction.East,
            Direction.North,
            Direction.NorthWest,
            Direction.South,
            Direction.SouthEast,
            Direction.West
        };
    }

    /// <summary>
    /// Drops multiple cartridges / shells on the floor
    /// Wraps EjectCasing to make it less toxic for bulk ejections
    /// </summary>
    /// <param name="entities"></param>
    public void EjectCasings(IEnumerable<EntityUid> entities, Direction[]? ejectDirections = null)
    {
        SetEjectDirections(ref ejectDirections);

        var soundPlayCount = 0;
        var playSound = true;

        foreach (var entity in entities)
        {
            EjectCasing(entity, playSound, ejectDirections);
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
        Direction[]? ejectDirections = null,
        AmmoComponent? ammoComponent = null)
    {
        SetEjectDirections(ref ejectDirections);

        const float ejectOffset = 1.8f;

        if (!Resolve(entity, ref ammoComponent)) return;

        var offsetPos = (_random.NextFloat(-ejectOffset, ejectOffset), _random.NextFloat(-ejectOffset, ejectOffset));

        var xform = EntityManager.GetComponent<TransformComponent>(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        xform.LocalRotation = _random.Pick(ejectDirections!).ToAngle();
        xform.Coordinates = coordinates;

        if (playSound)
            SoundSystem.Play(Filter.Pvs(entity), ammoComponent.SoundCollectionEject.GetSound(), coordinates, AudioParams.Default.WithVolume(-1));
    }

    // Rename when the other event is changed.
    public sealed class GunFireEvent : EntityEventArgs
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
