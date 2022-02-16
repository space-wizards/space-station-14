using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Stack
{
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, StackCountChangedEvent>(OnStackCountChanged);
        }

        private void OnStackCountChanged(EntityUid uid, StackComponent component, StackCountChangedEvent args)
        {
            // Dirty the UI now that the stack count has changed.
            component.UiUpdateNeeded = true;
        }
    }
}
