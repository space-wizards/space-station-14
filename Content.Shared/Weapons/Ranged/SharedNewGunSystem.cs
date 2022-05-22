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
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
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
        Dirty(gun);
    }

    protected bool AttemptShoot(EntityUid user, NewGunComponent gun)
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
        var fireRate = TimeSpan.FromSeconds(1f / gun.FireRate);

        while (gun.NextFire <= curTime)
        {
            gun.NextFire += fireRate;
            shots++;
        }

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        switch (gun.SelectiveFire)
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
        }

        DebugTools.Assert(shots >= 0);

        if (shots <= 0) return false;

        var oldShots = gun.ShotCounter;

        // Shoot confirmed
        gun.ShotCounter += shots;

        // Predicted sound moment
        PlaySound(gun, gun.SoundGunshot?.GetSound(), oldShots, user);
        Dirty(gun);

        return true;
    }

    protected abstract void PlaySound(NewGunComponent gun, string? sound, int shots, EntityUid? user = null);

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
    public sealed class RequestStopShootEvent : EntityEventArgs
    {
        public EntityUid Gun;
    }

    [Serializable, NetSerializable]
    protected sealed class NewGunComponentState : ComponentState
    {
        public string? SoundGunshot;
        public TimeSpan NextFire;
        public int ShotCounter;
    }
}
