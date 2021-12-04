using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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

        public static void AttachToGrandparent(this EntityUid entity)
        {
            AttachToGrandparent(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity));
        }
    }
}
