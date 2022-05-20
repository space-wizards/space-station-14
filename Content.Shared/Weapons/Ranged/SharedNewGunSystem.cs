using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

public abstract class SharedNewGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        Sawmill = Logger.GetSawmill("gun");
        SubscribeLocalEvent<NewGunComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, NewGunComponent component, ref ComponentGetState args)
    {
        args.State = new NewGunComponentState
        {
            NextFire = component.NextFire,
        };
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
    }

    protected virtual bool AttemptShoot(EntityUid user, NewGunComponent gun)
    {
        if (gun.FireRate <= 0f)
            return false;

        if (gun.ShootCoordinates == null)
            return false;

        var curTime = Timing.CurTime;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.NextFire > curTime)
            return false;

        // First shot
        if (gun.ShotCounter == 0 && gun.NextFire < curTime)
            gun.NextFire = curTime;

        var shots = 0;

        while (gun.NextFire <= curTime)
        {
            shots++;
            Sawmill.Debug($"Shooting at {gun.NextFire}");
            gun.NextFire += TimeSpan.FromSeconds(1f / gun.FireRate);
        }

        // Shoot confirmed
        gun.ShotCounter += shots;

        // Predicted sound moment
        PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(), user);
        Dirty(gun);

        return true;
    }

    protected abstract void PlaySound(EntityUid gun, string? sound, EntityUid? user = null);

    /// <summary>
    /// Raised on the client to indicate it'd like to shoot.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
        public MapCoordinates Coordinates;
    }

    [Serializable, NetSerializable]
    protected sealed class NewGunComponentState : ComponentState
    {
        public TimeSpan NextFire;
    }
}
