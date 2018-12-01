using System;
using SS14.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    public sealed class RangedWeaponComponent : SharedRangedWeaponComponent
    {
        private DateTime _lastFireTime;

        public Func<bool> WeaponCanFireHandler;
        public Func<IEntity, bool> UserCanFireHandler;
        public Action<IEntity, GridLocalCoordinates> FireHandler;

        private bool WeaponCanFire()
        {
            if (WeaponCanFireHandler != null && !WeaponCanFireHandler())
            {
                return false;
            }
            var span = DateTime.Now - _lastFireTime;
            return span.TotalSeconds * 1.05 >= 1 / FireRate;
        }

        private bool UserCanFire(IEntity user)
        {
            return WeaponCanFireHandler == null || UserCanFireHandler(user);
        }

        private void Fire(IEntity user, GridLocalCoordinates clickLocation)
        {
            _lastFireTime = DateTime.Now;
            FireHandler?.Invoke(user, clickLocation);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case FireMessage msg:
                    var playerMgr = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMgr.GetSessionByChannel(netChannel);
                    var user = session.AttachedEntity;
                    if (user == null || !user.TryGetComponent(out HandsComponent hands))
                    {
                        return;
                    }

                    if (hands.GetActiveHand.Owner != Owner)
                    {
                        return;
                    }

                    if (UserCanFire(user) && WeaponCanFire())
                    {
                        Fire(user, msg.Target);
                    }

                    break;
            }
        }
    }
}
