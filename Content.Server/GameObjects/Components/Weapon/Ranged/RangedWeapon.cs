using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class RangedWeaponComponent : SharedRangedWeaponComponent
    {
        private TimeSpan _lastFireTime;

        public Func<bool> WeaponCanFireHandler;
        public Func<IEntity, bool> UserCanFireHandler;
        public Action<IEntity, GridCoordinates> FireHandler;

        private const int MaxFireDelayAttempts = 2;

        private bool WeaponCanFire()
        {
            return WeaponCanFireHandler == null || WeaponCanFireHandler();
        }

        private bool UserCanFire(IEntity user)
        {
            return UserCanFireHandler == null || UserCanFireHandler(user);
        }

        private void Fire(IEntity user, GridCoordinates clickLocation)
        {
            _lastFireTime = IoCManager.Resolve<IGameTiming>().CurTime;
            FireHandler?.Invoke(user, clickLocation);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case SyncFirePosMessage msg:
                    var playerMgr = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMgr.GetSessionByChannel(netChannel);
                    var user = session.AttachedEntity;
                    if (user == null)
                    {
                        return;
                    }

                    _tryFire(user, msg.Target, 0);
                    break;
            }
        }

        private void _tryFire(IEntity user, GridCoordinates coordinates, int attemptCount)
        {
            if (!user.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand?.Owner != Owner)
            {
                return;
            }

            if(!user.TryGetComponent(out CombatModeComponent combat) || !combat.IsInCombatMode) {
                return;
            }

            if (!UserCanFire(user) || !WeaponCanFire())
            {
                return;
            }

            // Firing delays are quite complicated.
            // Sometimes the client's fire messages come in just too early.
            // Generally this is a frame or two of being early.
            // In that case we try them a few times the next frames to avoid having to drop them.
            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var span = curTime - _lastFireTime;
            if (span.TotalSeconds < 1 / FireRate)
            {
                if (attemptCount >= MaxFireDelayAttempts)
                {
                    return;
                }

                Timer.Spawn(TimeSpan.FromSeconds(1 / FireRate) - span,
                    () => _tryFire(user, coordinates, attemptCount + 1));
                return;
            }

            Fire(user, coordinates);
        }
    }
}
