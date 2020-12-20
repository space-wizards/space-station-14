using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems.EffectBlocker
{
    /// <summary>
    /// Utility methods to check if an effect is allowed to affect a specific entity.
    /// For actions see <see cref="ActionBlockerSystem"/>
    /// </summary>
    [UsedImplicitly]
    public class EffectBlockerSystem : EntitySystem
    {
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
