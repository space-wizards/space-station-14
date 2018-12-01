using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    public sealed class ClientRangedWeaponComponent : SharedRangedWeaponComponent
    {
        private DateTime _lastFireTime;
        private int _tick;

        public void TryFire(GridLocalCoordinates worldPos)
        {
            var span = DateTime.Now - _lastFireTime;
            if (span.TotalSeconds < 1 / FireRate)
            {
                return;
            }

            _lastFireTime = DateTime.Now;
            SendNetworkMessage(new FireMessage(worldPos, _tick++));
        }
    }
}
