using System.Collections.Generic;
using Content.Server.Actions.Actions;
using Content.Shared.Weapons.Guns;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Guns
{
    internal sealed class GunSystem : SharedGunSystem
    {
        private List<ShootMessage> _queuedShoot = new();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_queuedShoot.Count == 0) return;

            var currentTime = GameTiming.CurTime;

            for (var i = _queuedShoot.Count - 1; i >= 0; i--)
            {
                var msg = _queuedShoot[i];

                if (msg.Time > currentTime)
                {
                    break;
                }

                if (HandleShoot(msg))
                {
                    _queuedShoot.RemoveAt(i);
                }
            }
        }

        private bool HandleShoot(ShootMessage message)
        {
            // TODO: Validate
            if (!TryFire())
            {
                return true;
            }

            message.Shots -= shots;
            DebugTools.Assert(message.Shots >= 0);

            for (var i = 0; i < shots; i++)
            {
                SoundSystem.Play(Filter.Pvs(), )
            }

            if (message.Shots > 0)
            {
                return false;
            }

            return true;
        }
    }
}
