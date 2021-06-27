using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Guns
{
    public abstract class SharedGunSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        // TODO: Stuff. Play around and get prediction working first before doing SHEET.

        protected bool TryFire(IEntity user, SharedGunComponent gun, MapCoordinates coordinates, out int shots, TimeSpan currentTime)
        {
            shots = 0;

            if (gun.FireRate <= 0f)
            {
                return false;
            }

            if (gun.NextFire > currentTime)
            {
                return true;
            }

            var rate = gun.GetFireRate();

            while (gun.NextFire <= currentTime)
            {
                gun.NextFire += TimeSpan.FromSeconds(rate);
                shots++;
            }

            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ShootMessage : EntityEventArgs
    {
        public EntityUid GunUid { get; }
        public MapCoordinates Coordinates { get; }
        public int Shots { get; set; }
        public TimeSpan Time { get; set; }

        public ShootMessage(EntityUid uid, MapCoordinates coordinates, int shots, TimeSpan time)
        {
            GunUid = uid;
            Coordinates = coordinates;
            Time = time;
            Shots = shots;
        }
    }
}
