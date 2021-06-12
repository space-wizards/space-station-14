#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.EffectBlocker
{
    public static class EffectBlockerExtensions
    {
        public static bool CanFall(this IEntity entity)
        {
            return EffectBlockerSystem.CanFall(entity);
        }

        public static bool CanSlip(this IEntity entity)
        {
            return EffectBlockerSystem.CanSlip(entity);
        }
    }
}
