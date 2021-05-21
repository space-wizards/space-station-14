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
        protected override void OnStackCountChanged(EntityUid uid, SharedStackComponent component, StackCountChangedEvent args)
        {
            base.OnStackCountChanged(uid, component, args);

            if (component is not StackComponent stack)
                return;

            stack.DirtyUI();
        }
    }
}
