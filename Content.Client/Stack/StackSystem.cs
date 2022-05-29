using Content.Shared.Stacks;
using JetBrains.Annotations;

namespace Content.Client.Stack
{
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        public override void SetCount(EntityUid uid, int amount, SharedStackComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            base.SetCount(uid, amount, component);

            // TODO PREDICT ENTITY DELETION: This should really just be a normal entity deletion call.
            if (component.Count <= 0)
            {
                Transform(uid).DetachParentToNull();
                return;
            }

            // Dirty the UI now that the stack count has changed.
            if (component is StackComponent clientComp)
                clientComp.UiUpdateNeeded = true;
        }
    }
}
