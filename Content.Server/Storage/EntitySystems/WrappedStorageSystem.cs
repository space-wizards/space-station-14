using Content.Server.Storage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public class WrappedStorageSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
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
                    if (_containerSystem.TryGetContainingContainer(args.Target, out var container))
                    {
                        // Drop 'wrapped object' because when i'm unwraping item, he always spawns on other hand or drop on floor
                        Transform(args.Target).AttachToGridOrMap();
                        var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                        //Insert unpacked item
                        container.Insert(ent);
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

            verb.Text = Loc.GetString("packaged-item-verb-unpack");

            args.Verbs.Add(verb);
        }
    }
}
