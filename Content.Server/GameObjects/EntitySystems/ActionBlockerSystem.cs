using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface IActionBlocker
    {
        bool CanMove();

        bool CanInteract();

        bool CanUse();

        bool CanThrow();

        bool CanSpeak();
    }

    public class ActionBlockerSystem : EntitySystem
    {
        public static bool CanMove(IEntity entity)
        {
            bool canmove = true;
            foreach(var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canmove &= actionblockercomponents.CanMove(); //sets var to false if false
            }
            return canmove;
        }

        public static bool CanInteract(IEntity entity)
        {
            bool caninteract = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                caninteract &= actionblockercomponents.CanInteract();
            }
            return caninteract;
        }

        public static bool CanUse(IEntity entity)
        {
            bool canuse = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canuse &= actionblockercomponents.CanUse();
            }
            return canuse;
        }

        public static bool CanThrow(IEntity entity)
        {
            bool canthrow = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canthrow &= actionblockercomponents.CanThrow();
            }
            return canthrow;
        }

        public static bool CanSpeak(IEntity entity)
        {
            bool canspeak = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canspeak &= actionblockercomponents.CanSpeak();
            }
            return canspeak;
        }
    }
}
