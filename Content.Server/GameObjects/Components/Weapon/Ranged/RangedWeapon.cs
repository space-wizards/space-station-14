using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class RangedWeaponComponent : SharedRangedWeaponComponent
    {
        private TimeSpan _lastFireTime;

        public Func<bool> WeaponCanFireHandler;
        public Func<IEntity, bool> UserCanFireHandler;
        public Action<IEntity, GridCoordinates> FireHandler;

        private bool WeaponCanFire()
        {
            return WeaponCanFireHandler == null || WeaponCanFireHandler();
        }

        private bool UserCanFire(IEntity user)
        {
            return (UserCanFireHandler == null || UserCanFireHandler(user)) && ActionBlockerSystem.CanAttack(user);
        }

        private void Fire(IEntity user, GridCoordinates clickLocation)
        {
            _lastFireTime = IoCManager.Resolve<IGameTiming>().CurTime;
            FireHandler?.Invoke(user, clickLocation);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case SyncFirePosMessage msg:
                    var user = session.AttachedEntity;
                    if (user == null)
                    {
                        return;
                    }

                    _tryFire(user, msg.Target);
                    break;
            }
        }

        // Probably shouldn't be a separate method but don't want anything except NPCs calling this,
        // and currently ranged combat is handled via player only messages
        public void AiFire(IEntity entity, GridCoordinates coordinates)
        {
            if (!entity.HasComponent<AiControllerComponent>())
            {
                throw new InvalidOperationException("Only AIs should call AiFire");
            }

            _tryFire(entity, coordinates);
        }

        private void _tryFire(IEntity user, GridCoordinates coordinates)
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

            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var span = curTime - _lastFireTime;
            if (span.TotalSeconds < 1 / FireRate)
            {
                return;
            }

            Fire(user, coordinates);
        }
    }
}
