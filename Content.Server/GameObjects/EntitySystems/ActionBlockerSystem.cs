using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface IActionBlocker
    {
        bool CanMove();

        bool CanInteract();

        bool CanUse();
    }

    public class ActionBlockerSystem : EntitySystem
    {
        public static bool CanMove(IEntity entity)
        {
            bool canmove = true;
            foreach(var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canmove &= actionblockercomponents.CanMove(); //if false, sets var to false
            }
            return canmove;
        }

        public static bool CanInteract(IEntity entity)
        {
            bool caninteract = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                caninteract &= actionblockercomponents.CanInteract(); //if false, sets var to false
            }
            return caninteract;
        }

        public static bool CanUse(IEntity entity)
        {
            bool canuse = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canuse &= actionblockercomponents.CanUse(); //if false, sets var to false
            }
            return canuse;
        }
    }
}
