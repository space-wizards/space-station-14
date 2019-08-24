using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Status
{
    /// <summary>
    ///     Status Effects is a helper class, serving as a library of possible effects to apply to an entity.
    /// </summary>
    ///
    public interface IStatusEffect : IActionBlocker
    {
        void EnterStatus(IEntity entity);

        void ExitStatus(IEntity entity);
    }

    public struct RestrainedStatus : IStatusEffect
    {
        public bool CanMove()
        {
            return true;
        }

        public bool CanInteract()
        {
            return false;
        }

        public bool CanUse()
        {
            return false;
        }

        public bool CanThrow()
        {
            return false;
        }

        public void EnterStatus(IEntity entity)
        {
        }

        public void ExitStatus(IEntity entity)
        {
        }
    }
}
