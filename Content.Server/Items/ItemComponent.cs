using System.Collections.Generic;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    {
        public override void RemovedFromSlot()
        {
            foreach (var component in IoCManager.Resolve<IEntityManager>().GetComponents<ISpriteRenderableComponent>(Owner))
            {
                component.Visible = true;
            }
        }

        public override void EquippedToSlot()
        {
            foreach (var component in IoCManager.Resolve<IEntityManager>().GetComponents<ISpriteRenderableComponent>(Owner))
            {
                component.Visible = false;
            }
        }
    }
}

