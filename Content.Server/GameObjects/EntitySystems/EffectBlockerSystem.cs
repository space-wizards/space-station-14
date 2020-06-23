using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface IEffectBlocker
    {
        bool CanFall() => true;
    }

    public class EffectBlockerSystem : EntitySystem
    {
        public static bool CanFall(IEntity entity)
        {
            var canFall = true;
            foreach (var blocker in entity.GetAllComponents<IEffectBlocker>())
            {
                canFall &= blocker.CanFall();
            }

            return canFall;
        }
    }
}
