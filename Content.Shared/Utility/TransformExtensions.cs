using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Content.Shared.Utility
{
    public static class TransformExtensions
    {
        public static void AttachToGrandparent(this ITransformComponent transform)
        {
            var grandParent = transform.Parent?.Parent;

            if (grandParent == null)
            {
                transform.AttachToGridOrMap();
                return;
            }

            transform.AttachParent(grandParent);
        }

        public static void AttachToGrandparent(this IEntity entity)
        {
            AttachToGrandparent(entity.Transform);
        }
    }
}
