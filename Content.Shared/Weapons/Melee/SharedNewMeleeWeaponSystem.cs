using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedNewMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("melee");

        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentHandleState>(OnHandleState);
        SubscribeAllEvent<StartAttackEvent>(OnAttackStart);
        SubscribeAllEvent<ReleaseAttackEvent>(OnReleaseAttack);
        SubscribeAllEvent<StopAttackEvent>(StopAttack);
    }

    /// <summary>
    /// Raised when an attack hold is started.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class StartAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
    }

    /// <summary>
    /// Raised when an attack hold is ended after windup.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class ReleaseAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
        public EntityCoordinates Coordinates;
    }

    /// <summary>
    /// Raised when an attack is stopped and no attack should occur.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class StopAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
    }

    [Serializable, NetSerializable]
    protected sealed class NewMeleeWeaponComponentState : ComponentState
    {
        public TimeSpan NextAttack;
    }

    protected virtual void OnAttackStart(StartAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        Sawmill.Debug($"Started weapon attack");
    }

    private void OnReleaseAttack(ReleaseAttackEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<NewMeleeWeaponComponent>(ev.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        AttemptAttack(args.SenderSession.AttachedEntity.Value, weapon, ev.Coordinates);
    }

    private void StopAttack(StopAttackEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<NewMeleeWeaponComponent>(ev.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        if (weapon.WindupAccumulator <= 0f)
            return;

        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    private void OnGetState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new NewMeleeWeaponComponentState
        {
            NextAttack = component.NextAttack,
        };
    }

    private void OnHandleState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewMeleeWeaponComponentState state)
            return;

        component.NextAttack = state.NextAttack;
    }

    public NewMeleeWeaponComponent? GetWeapon(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            return null;
        }

        if (!EntityManager.TryGetComponent(held, out NewMeleeWeaponComponent? gun))
            return null;

        if (!CombatMode.IsInCombatMode(entity))
            return null;

        return gun;
    }

    private void AttemptAttack(EntityUid user, NewMeleeWeaponComponent weapon, EntityCoordinates coordinates)
    {
        if (weapon.WindupAccumulator < weapon.WindupTime)
            return;

        var toCoordinates = weapon.ShootCoordinates;

        if (toCoordinates == null) return;

        if (TagSystem.HasTag(user, "GunsDisabled"))
        {
            Popup(Loc.GetString("gun-disabled"), user, user);
            return;
        }

        var curTime = Timing.CurTime;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (weapon.NextFire > curTime) return;

        // First shot
        if (weapon.ShotCounter == 0 && weapon.NextFire < curTime)
            weapon.NextFire = curTime;

        var shots = 0;
        var lastFire = weapon.NextFire;
        var fireRate = TimeSpan.FromSeconds(1f / weapon.FireRate);

        while (weapon.NextFire <= curTime)
        {
            weapon.NextFire += fireRate;
            shots++;
        }

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        switch (weapon.SelectedMode)
        {
            case SelectiveFire.SemiAuto:
                shots = Math.Min(shots, 1 - weapon.ShotCounter);
                break;
            case SelectiveFire.Burst:
                shots = Math.Min(shots, 3 - weapon.ShotCounter);
                break;
            case SelectiveFire.FullAuto:
                break;
            default:
                throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {weapon.SelectedMode}!");
        }

        var fromCoordinates = Transform(user).Coordinates;
        // Remove ammo
        var ev = new TakeAmmoEvent(shots, new List<IShootable>(), fromCoordinates, user);

        // Listen it just makes the other code around it easier if shots == 0 to do this.
        if (shots > 0)
            RaiseLocalEvent(weapon.Owner, ev, false);

        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);
        UpdateAmmoCount(weapon.Owner);

        // Even if we don't actually shoot update the ShotCounter. This is to avoid spamming empty sounds
        // where the gun may be SemiAuto or Burst.
        weapon.ShotCounter += shots;

        if (ev.Ammo.Count <= 0)
        {
            // Play empty gun sounds if relevant
            // If they're firing an existing clip then don't play anything.
            if (shots > 0)
            {
                // Don't spam safety sounds at gun fire rate, play it at a reduced rate.
                // May cause prediction issues? Needs more tweaking
                weapon.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, weapon.NextFire.TotalSeconds));
                PlaySound(weapon.Owner, weapon.SoundEmpty?.GetSound(Random, ProtoManager), user);
                Dirty(weapon);
                return;
            }

            return;
        }

        // Shoot confirmed - sounds also played here in case it's invalid (e.g. cartridge already spent).
        Shoot(weapon, ev.Ammo, fromCoordinates, toCoordinates.Value, user);
        Dirty(weapon);
    }
}
