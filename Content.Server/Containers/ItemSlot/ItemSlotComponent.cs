using Content.Shared.ActionBlocker;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Containers.ItemSlot
{
    [RegisterComponent]
    public class ItemSlotComponent : Component
    {
        public override string Name => "ItemSlot";

        [ViewVariables] [DataField("slotName")] public string SlotName = "item_slot";
        [ViewVariables] [DataField("item")] public string? StartingItem;
        [ViewVariables] [DataField("whitelist")] public EntityWhitelist? Whitelist = null;
        [ViewVariables] [DataField("insertSound")] public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");
        [ViewVariables] [DataField("ejectSound")] public SoundSpecifier EjectIdSound = new SoundPathSpecifier("/Audio/Machines/id_swipe.ogg");
        [ViewVariables] [DataField("verbTextID")] public string? CustomVerbTextID;

        [ViewVariables] public ContainerSlot ContainerSlot = default!;

        [Verb]
        public sealed class EjectItemVerb : Verb<ItemSlotComponent>
        {
            protected override void GetData(IEntity user, ItemSlotComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var item = component.ContainerSlot.ContainedEntity;
                if (item == null)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.CustomVerbTextID == null)
                    data.Text = Loc.GetString("eject-item-verb-text-default", ("item", item.Name));
                else
                    data.Text = component.CustomVerbTextID;

                data.Visibility = VerbVisibility.Visible;
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, ItemSlotComponent component)
            {
                EntitySystem.Get<ItemSlotSystem>().TryEjectContent(component, user);
            }
        }
    }
}
