using Content.Client.Hands;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Client.Items.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    {
        protected override void OnEquippedPrefixChange()
        {
            if (!Owner.TryGetContainer(out var container))
                return;

            if (container.Owner.TryGetComponent(out HandsComponent? hands))
                hands.UpdateHandVisualizer();
        }
    }
}
