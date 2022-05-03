using Content.Server.Storage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public class WrappedStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WrappedStorageComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WrappedStorageComponent, GetVerbsEvent<AlternativeVerb>>(AddUnpackVerb);
        }

        private void OnInit(EntityUid uid, WrappedStorageComponent component, ComponentInit args)
        {
            component.ItemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(uid, "wrap", out _);
        }

        private void Unpack(EntityUid uid, WrappedStorageComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (uid != null)
            {
                Comp<PhysicsComponent>(uid).CanCollide = false; // Turn off collider, because 'wraped object' move unwraped obj

                if (component.ItemContainer.ContainedEntity != null)
                {
                    args.User.TryGetContainer(out var userContainer); //Check if item in hand, if in hand unpack and put in hand
                    if (args.Target.TryGetContainer(out var container) && container != userContainer)
                    {
                        var handComp = Comp<SharedHandsComponent>(args.User);
                        // Drop 'wrapped object' because when i'm unwraping item, he always spawns on other hand or drop on floor
                        handComp.Drop(uid);
                        var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                        handComp.PutInHandOrDrop(ent);
                    }
                    else
                    {
                        var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                        Comp<TransformComponent>(ent).Coordinates = Comp<TransformComponent>(uid).Coordinates;
                    }
                }
                QueueDel(uid);
            }
        }

        private void AddUnpackVerb(EntityUid uid, WrappedStorageComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            AlternativeVerb verb = new();
            verb.Act = () => Unpack(uid, component, args);
            verb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

            // if the item already in a container (that is not the same as the user's), then change the text.
            // this occurs when the item is in their inventory or in an open backpack
            verb.Text = "Unpack"; // TODO: Make Loc.GetString

            args.Verbs.Add(verb);
        }
    }
}
