#nullable enable
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

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

        public override bool TryPutInHand(IEntity user)
        {
            if (!CanPickup(user))
                return false;

            if (!user.TryGetComponent(out IHandsComponent? hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            hands.PutInHand(this, activeHand, false);
            return true;
        }

        [Verb]
        public sealed class PickUpVerb : Verb<ItemComponent>
        {
            protected override void GetData(IEntity user, ItemComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                    component.Owner.IsInContainer() ||
                    !component.CanPickup(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("pick-up-verb-get-data-text");
            }

            protected override void Activate(IEntity user, ItemComponent component)
            {
                if (user.TryGetComponent(out HandsComponent? hands) && !hands.IsHolding(component.Owner))
                {
                    hands.PutInHand(component);
                }
            }
        }
    }
}
