using Content.Client.Hands;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(container.Owner, out HandsComponent? hands))
                hands.UpdateHandVisualizer();
        }
    }
}
