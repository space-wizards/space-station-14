using Content.Client.Items;
using Content.Shared.Stacks;
using JetBrains.Annotations;

namespace Content.Client.Stack
{
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StackComponent, ItemStatusCollectMessage>(OnItemStatus);
        }

        private void OnItemStatus(EntityUid uid, StackComponent component, ItemStatusCollectMessage args)
        {
            args.Controls.Add(new StackStatusControl(component));
        }

        public override void SetCount(EntityUid uid, int amount, StackComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            base.SetCount(uid, amount, component);

            // TODO PREDICT ENTITY DELETION: This should really just be a normal entity deletion call.
            if (component.Count <= 0)
            {
                Xform.DetachParentToNull(Transform(uid));
                return;
            }

            // Dirty the UI now that the stack count has changed.
            if (component is StackComponent clientComp)
                clientComp.UiUpdateNeeded = true;
        }
    }
}
