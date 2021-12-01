using Content.Shared.Item;
using Robust.Shared.GameObjects;

namespace Content.Client.Items.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    {}
}
