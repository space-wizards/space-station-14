using System.Collections.Generic;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent { }
}

