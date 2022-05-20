using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged;

public abstract class SharedNewGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
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

    /// <summary>
    /// Tries to shoot a single time at the specified coordinates.
    /// </summary>
    public bool TryShoot(EntityUid user, NewGunComponent gun, MapCoordinates coordinates, out int shots)
    {
        // TODO: Validate method
        if (gun.FireRate <= 0f)
        {
            shots = 0;
            return false;
        }

        gun.AttemptedShotLastTick = true;
        var curTime = Timing.CurTime;

        if (gun.NextFire > curTime)
        {
            shots = 0;
            return false;
        }

        shots = 0;

        while (gun.NextFire <= curTime)
        {
            shots++;
            gun.NextFire += TimeSpan.FromSeconds(1f / gun.FireRate);
        }

        // Shoot confirmed
        gun.ShotCounter += shots;
        Sawmill.Debug($"Set next fire to {gun.NextFire}");

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
        public bool FirstShot;
        public EntityUid Gun;
        public MapCoordinates Coordinates;
        public int Shots;
    }

    [Serializable, NetSerializable]
    protected sealed class NewGunComponentState : ComponentState
    {
        public TimeSpan NextFire;
    }
}
