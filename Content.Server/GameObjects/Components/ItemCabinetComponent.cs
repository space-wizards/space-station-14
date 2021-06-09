using System.Collections;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Utility;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    /// <summary>
    ///     Used for entities that can hold one item that fits the whitelist, which can be extracted by interacting with
    ///     the entity, and can have an item fitting the whitelist placed back inside
    /// </summary>
    [RegisterComponent]
    public class ItemCabinetComponent : Component
    {
        public override string Name => "ItemCabinet";

        /// <summary>
        ///     Sound to be played when the cabinet door is opened.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("doorSound")]
        public string? DoorSound { get; set; }

        /// <summary>
        ///     The prototype that should be spawned inside the cabinet when it is map initialized.
        /// </summary>
        [ViewVariables]
        [DataField("spawnPrototype")]
        public string? SpawnPrototype { get; set; }

        /// <summary>
        ///     A whitelist defining which entities are allowed into the cabinet.
        /// </summary>
        [ViewVariables]
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist = null;

        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

        /// <summary>
        ///     Whether the cabinet is currently open or not.
        /// </summary>
        [ViewVariables]
        [DataField("opened")]
        public bool Opened { get; set; } = false;

        [Verb]
        public sealed class EjectItemFromCabinetVerb : Verb<ItemCabinetComponent>
        {
            protected override void GetData(IEntity user, ItemCabinetComponent component, VerbData data)
            {
                if (component.ItemContainer.ContainedEntity == null || !component.Opened || !ActionBlockerSystem.CanInteract(user))
                    data.Visibility = VerbVisibility.Invisible;
                else
                {
                    data.Text = Loc.GetString("comp-item-cabinet-eject-verb-text");
                    data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                    data.Visibility = VerbVisibility.Visible;
                }
            }

            protected override void Activate(IEntity user, ItemCabinetComponent component)
            {
                component.Owner.EntityManager.EventBus.RaiseLocalEvent(component.Owner.Uid, new TryEjectItemCabinetEvent(user), false);
            }
        }

        [Verb]
        public sealed class ToggleItemCabinetVerb : Verb<ItemCabinetComponent>
        {
            protected override void GetData(IEntity user, ItemCabinetComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                    data.Visibility = VerbVisibility.Invisible;
                else
                {
                    data.Text = Loc.GetString(component.Opened ? "comp-item-cabinet-close-verb-text" : "comp-item-cabinet-open-verb-text");
                    data.IconTexture = component.Opened ? "/Textures/Interface/VerbIcons/close.svg.192dpi.png" : "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
                    data.Visibility = VerbVisibility.Visible;
                }
            }

            protected override void Activate(IEntity user, ItemCabinetComponent component)
            {
                component.Owner.EntityManager.EventBus.RaiseLocalEvent(component.Owner.Uid, new ToggleItemCabinetEvent(), false);
            }
        }
    }
}
