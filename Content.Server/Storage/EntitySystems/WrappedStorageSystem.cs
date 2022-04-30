using Content.Server.Clothing.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Acts;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems
{
    public class WrappedStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WrappedStorageComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WrappedStorageComponent, GetVerbsEvent<InteractionVerb>>(AddUnpackVerb);
        }

        private void OnInit(EntityUid uid, WrappedStorageComponent component, ComponentInit args)
        {
            component.ItemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(uid, "wrap", out _);
        }

        private void Unpack(EntityUid uid, WrappedStorageComponent component, GetVerbsEvent<InteractionVerb> args) // TODO: make call by alt-click
        {
            if (component.ItemContainer.ContainedEntity != null)
            {
                args.User.TryGetContainer(out var userContainer); //Check if item in hand, if in hand unpack and put in hand
                if (args.Target.TryGetContainer(out var container) && container != userContainer)
                {
                    var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                    Comp<SharedHandsComponent>(args.User).PutInHandOrDrop(ent);
                }
                else
                {
                    var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                    Comp<TransformComponent>(ent).Coordinates = Comp<TransformComponent>(uid).Coordinates; // Crate respawns bit lower than package
                }
            }
            QueueDel(uid);
        }

        private void AddUnpackVerb(EntityUid uid, WrappedStorageComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            InteractionVerb verb = new();
            verb.Act = () => Unpack(uid, component, args);
            verb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

            // if the item already in a container (that is not the same as the user's), then change the text.
            // this occurs when the item is in their inventory or in an open backpack
            verb.Text = "Unpack"; // TODO: Make Loc.GetString

            args.Verbs.Add(verb);
        }
    }
}
