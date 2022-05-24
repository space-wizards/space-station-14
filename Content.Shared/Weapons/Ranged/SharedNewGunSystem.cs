using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly SharedAdminLogSystem Logs = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

    protected const float MuzzleFlashLifetime = 1f;
    protected const float InteractNextFire = 0.3f;
    public const float EjectOffset = 0.4f;
    public string AmmoExamineColor = "yellow";
    public string FireRateExamineColor = "yellow";
    public string SafetyExamineColor = "lightgreen";
    public string ModeExamineColor = "cyan";

    public override void Initialize()
    {
        Sawmill = Logger.GetSawmill("gun");
        SubscribeLocalEvent<NewGunComponent, ComponentGetState>(OnGetState);
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<NewGunComponent, ComponentHandleState>(OnHandleState);

        // Ammo providers
        InitializeBallistic();
        InitializeBattery();
        InitializeChamberMagazine();
        InitializeMagazine();
        InitializeRevolver();

        // Interactions
        SubscribeLocalEvent<NewGunComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<NewGunComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<NewGunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NewGunComponent, CycleModeEvent>(OnCycleMode);
    }
    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null) return;

        var gun = GetGun(user.Value);

        if (gun?.Owner != msg.Gun) return;

        gun.ShootCoordinates = msg.Coordinates;
        Sawmill.Debug($"Set shoot coordinates to {gun.ShootCoordinates}");
        AttemptShoot(user.Value, gun);
    }

    private void OnStopShootRequest(RequestStopShootEvent ev)
    {
        // TODO validate input
        StopShooting(Comp<NewGunComponent>(ev.Gun));
    }

    private void OnGetState(EntityUid uid, NewGunComponent component, ref ComponentGetState args)
    {
        args.State = new NewGunComponentState
        {
            NextFire = component.NextFire,
            ShotCounter = component.ShotCounter,
            FakeAmmo = component.FakeAmmo,
            SelectiveFire = component.SelectedMode,
            AvailableSelectiveFire = component.AvailableModes,
        };
    }

    private void OnHandleState(EntityUid uid, NewGunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewGunComponentState state) return;

        Sawmill.Debug($"Handle state: setting shot count from {component.ShotCounter} to {state.ShotCounter}");
        component.NextFire = state.NextFire;
        component.ShotCounter = state.ShotCounter;
        component.FakeAmmo = state.FakeAmmo;
        component.SelectedMode = state.SelectiveFire;
        component.AvailableModes = state.AvailableSelectiveFire;
    }

    protected NewGunComponent? GetGun(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            return null;
        }

        if (!EntityManager.TryGetComponent(held, out NewGunComponent? gun))
            return null;

        if (!_combatMode.IsInCombatMode(entity))
            return null;

        return gun;
    }

    protected void StopShooting(NewGunComponent gun)
    {
        if (gun.ShotCounter == 0) return;

        Sawmill.Debug($"Stopped shooting {ToPrettyString(gun.Owner)}");
        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(gun);
    }

    private void AttemptShoot(EntityUid user, NewGunComponent gun)
    {
        if (gun.FireRate <= 0f) return;

        var toCoordinates = gun.ShootCoordinates;

        if (toCoordinates == null) return;

        var curTime = Timing.CurTime;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.NextFire > curTime) return;

        // First shot
        if (gun.ShotCounter == 0 && gun.NextFire < curTime)
            gun.NextFire = curTime;

        var shots = 0;
        var fireRate = TimeSpan.FromSeconds(1f / gun.FireRate);

        while (gun.NextFire <= curTime)
        {
            gun.NextFire += fireRate;
            shots++;
        }

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        switch (gun.SelectedMode)
        {
            case SelectiveFire.Safety:
                shots = 0;
                break;
            case SelectiveFire.SemiAuto:
                shots = Math.Min(shots, 1 - gun.ShotCounter);
                break;
            case SelectiveFire.Burst:
                shots = Math.Min(shots, 3 - gun.ShotCounter);
                break;
            case SelectiveFire.FullAuto:
                break;
            default:
                throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {gun.SelectedMode}!");
        }

        var fromCoordinates = Transform(user).Coordinates;

        // TODO: Just change this to use Ammo
        // Make cartridge inherit ammo
        // Have a thing that returns the bullets from ammo (AKA handles cloning for cartridges) + deletion.

        // Remove ammo
        var ev = new TakeAmmoEvent(shots, new List<IShootable>(), fromCoordinates);

        RaiseLocalEvent(gun.Owner, ev);
        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);

        if (ev.Ammo.Count <= 0)
        {
            // Play empty gun sounds if relevant
            // If they're firing an existing clip then don't play anything.
            if (gun.ShotCounter == 0 && (gun.SelectedMode == SelectiveFire.Safety || shots > 0))
            {
                PlaySound(gun, gun.SoundEmpty?.GetSound(), user);
                Dirty(gun);
                return;
            }

            return;
        }

        // Shoot confirmed
        gun.ShotCounter += ev.Ammo.Count;

        Shoot(ev.Ammo, fromCoordinates, toCoordinates.Value, user);

        // Predicted sound moment
        PlaySound(gun, gun.SoundGunshot?.GetSound(), user);
        Dirty(gun);
    }

    public void Shoot(
        EntityUid ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null)
    {
        var shootable = EnsureComp<NewAmmoComponent>(ammo);
        Shoot(new List<IShootable>(1) { shootable }, fromCoordinates, toCoordinates, user);
    }

    public abstract void Shoot(
        List<IShootable> ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null);

    public void Shoot(
        IShootable ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null)
    {
        Shoot(new List<IShootable>(1) { ammo }, fromCoordinates, toCoordinates, user);
    }

    protected abstract void PlaySound(NewGunComponent gun, string? sound, EntityUid? user = null);

    protected abstract void Popup(string message, NewGunComponent gun, EntityUid? user);

    /// <summary>
    /// Drops a single cartridge / shell
    /// </summary>
    public void EjectCartridge(
        EntityUid entity,
        bool playSound = true)
    {
        var offsetPos = (Random.NextVector2(EjectOffset));

        var xform = Transform(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        xform.LocalRotation = Random.NextAngle();
        xform.Coordinates = coordinates;

        string? sound = null;

        if (TryComp<CartridgeAmmoComponent>(entity, out var cartridge))
        {
            sound = cartridge.EjectSound?.GetSound();
        }

        if (sound != null && playSound)
            SoundSystem.Play(Filter.Pvs(entity, entityManager: EntityManager), sound, coordinates, AudioHelpers.WithVariation(0.05f).WithVolume(-1f));
    }

    public void MuzzleFlash(EntityUid entity, NewAmmoComponent component, Angle angle, EntityUid? user = null)
    {
        var sprite = component.MuzzleFlash?.ToString();

        if (sprite == null)
        {
            return;
        }

        var time = Timing.CurTime;
        var deathTime = time + TimeSpan.FromSeconds(MuzzleFlashLifetime);
        // Offset the sprite so it actually looks like it's coming from the gun
        var offset = new Vector2(0.0f, -0.5f);

        var message = new EffectSystemMessage
        {
            EffectSprite = sprite,
            Born = time,
            DeathTime = deathTime,
            AttachedEntityUid = entity,
            AttachedOffset = offset,
            //Rotated from east facing
            Rotation = -MathF.PI / 2f,
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        CreateEffect(message, user);

        /* TODO: Fix rotation when shooting sideways. This was the closest I got but still had issues.
         * var time = _gameTiming.CurTime;
        var deathTime = time + TimeSpan.FromMilliseconds(200);
        var entityRotation = EntityManager.GetComponent<TransformComponent>(entity).WorldRotation;
        var localAngle = entityRotation - (angle + MathF.PI / 2f);
        // Offset the sprite so it actually looks like it's coming from the gun
        var offset = localAngle.RotateVec(new Vector2(0.0f, -0.5f));

        var message = new EffectSystemMessage
        {
            EffectSprite = component.MuzzleFlashSprite.ToString(),
            Born = time,
            DeathTime = deathTime,
            AttachedEntityUid = entity,
            AttachedOffset = offset,
            //Rotated from east facing
            Rotation = (float) (localAngle - MathF.PI / 2),
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 255), 1.0f),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };
         */
    }

    protected abstract void CreateEffect(EffectSystemMessage message, EntityUid? user = null);

    /// <summary>
    /// Raised on a gun when it would like to take the specified amount of ammo.
    /// </summary>
    public sealed class TakeAmmoEvent : EntityEventArgs
    {
        public readonly int Shots;
        public List<IShootable> Ammo;

        /// <summary>
        /// Coordinates to spawn the ammo at.
        /// </summary>
        public EntityCoordinates Coordinates;

        public TakeAmmoEvent(int shots, List<IShootable> ammo, EntityCoordinates coordinates)
        {
            Shots = shots;
            Ammo = ammo;
            Coordinates = coordinates;
        }
    }

    /// <summary>
    /// Raised on the client to indicate it'd like to shoot.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
        public EntityCoordinates Coordinates;
    }

    [Serializable, NetSerializable]
    public sealed class RequestStopShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
    }

    [Serializable, NetSerializable]
    protected sealed class NewGunComponentState : ComponentState
    {
        public TimeSpan NextFire;
        public int ShotCounter;
        public int FakeAmmo;
        public SelectiveFire SelectiveFire;
        public SelectiveFire AvailableSelectiveFire;
    }

    /// <summary>
    /// Interface that says this can be shot from a gun. Exists to facilitate hitscan OR prototype shooting.
    /// </summary>
    public interface IShootable {}
}
