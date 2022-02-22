using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Items.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [Virtual]
    public class ItemComponent : SharedItemComponent { }
}
