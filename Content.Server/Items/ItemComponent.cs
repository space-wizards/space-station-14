using Content.Shared.Item;
using Robust.Shared.GameObjects;

namespace Content.Server.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    { }
}

