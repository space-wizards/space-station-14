using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    public sealed class ClientRangedWeaponComponent : SharedRangedWeaponComponent
    {
        private TimeSpan _lastFireTime;
        private int _tick;

        public void TryFire(GridCoordinates worldPos)
        {
            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var span = curTime - _lastFireTime;
            if (span.TotalSeconds < 1 / FireRate)
            {
                return;
            }

            Logger.Debug("Delay: {0}", span.TotalSeconds);
            _lastFireTime = curTime;
            SendNetworkMessage(new FireMessage(worldPos, _tick++));
        }
    }
}
