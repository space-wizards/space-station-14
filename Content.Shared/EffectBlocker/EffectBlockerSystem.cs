using System;
using Content.Shared.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.EffectBlocker
{
    /// <summary>
    /// Utility methods to check if an effect is allowed to affect a specific entity.
    /// For actions see <see cref="ActionBlockerSystem"/>
    /// </summary>
    [UsedImplicitly]
    public class EffectBlockerSystem : EntitySystem
    {
        // TODO: Make these methods not static. Maybe move them to their relevant EntitySystems?
        // TODO: Add EntityUid overloads.

        public static bool CanFall(IEntity entity)
        {
            var canFall = true;

            foreach (var blocker in entity.GetAllComponents<IEffectBlocker>())
            {
                canFall &= blocker.CanFall(); // Sets var to false if false
            }

            return canFall;
        }

        public static bool CanSlip(IEntity entity)
        {
            var canSlip = true;

            foreach (var blocker in entity.GetAllComponents<IEffectBlocker>())
            {
                canSlip &= blocker.CanSlip(); // Sets var to false if false
            }

            return canSlip;
        }
    }
}
