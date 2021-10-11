using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Items
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public class ItemComponent : SharedItemComponent
    {
        public override void RemovedFromSlot()
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = true;
            }
        }

        public override void EquippedToSlot()
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = false;
            }
        }
    }
}

