using Robust.Shared.GameObjects;

namespace Content.Shared.Transform
{
    public static class TransformExtensions
    {
        public static void AttachToGrandparent(this TransformComponent transform)
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
