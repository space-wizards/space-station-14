using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

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

            _lastFireTime = curTime;
            SendNetworkMessage(new FireMessage(worldPos, _tick++));
        }
    }
}
