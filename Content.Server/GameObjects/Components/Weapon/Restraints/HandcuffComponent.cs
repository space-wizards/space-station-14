using System;
using Content.Shared.GameObjects.Components.Weapons.Restraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Weapon.Restraints
{
    public class HandcuffComponent : SharedHandcuffComponent
    {
        private TimeSpan _lastRestrainedTime;
        private HandcuffComponent _restrainedStatus;

        public Func<IEntity, bool> UserIsRestrainedHandler;


        private bool UserIsRestrained(IEntity user)
        {
            return UserIsRestrainedHandler == null || UserIsRestrainedHandler(user);
        }

        private void Restrain(IEntity user)
        {
            _lastRestrainedTime = IoCManager.Resolve<IGameTiming>().CurTime;
            _restrainedStatus = IoCManager.Resolve<IEntity>().AddComponent<HandcuffComponent>();
            UserIsRestrainedHandler?.Invoke(user);
        }

        private void _tryRestrain(IEntity user)
        {
            if (!user.TryGetComponent(out HandcuffComponent handcuff))
            {
                return;
            }

            if (!UserIsRestrained(user))
            {
                return;
            }
            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var restraint = IoCManager.Resolve<IEntity>().GetComponent<HandcuffComponent>();
            var timeToRestrain = restraint.BreakoutTime;
            var span = curTime - _lastRestrainedTime;

            if (span == timeToRestrain)
            {
                IoCManager.Resolve<IEntity>().RemoveComponent<HandcuffComponent>();
            }

            Restrain(user);
        }
    }
}
