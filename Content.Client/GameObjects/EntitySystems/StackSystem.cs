using Content.Client.GameObjects.Components;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class StackSystem : SharedStackSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, StackCountChangedEvent>(OnStackCountChanged);
        }

        private void OnStackCountChanged(EntityUid uid, StackComponent component, StackCountChangedEvent args)
        {
            // Dirty the UI now that the stack count has changed.
            component.DirtyUI();
        }
    }
}
