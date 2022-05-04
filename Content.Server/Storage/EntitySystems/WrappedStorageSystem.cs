using Content.Server.Storage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    public class WrappedStorageSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
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
                        if (TryFindHandWithItem(uid, handComp, out var hand))
                        {
                            if (hand != null)
                            {
                                _handsSystem.DoDrop(uid, hand);
                                var ent = (EntityUid) component.ItemContainer.ContainedEntity;
                                _handsSystem.TryPickupAnyHand(args.User,ent);
                            }
                        }
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

        private bool TryFindHandWithItem(EntityUid target,SharedHandsComponent handComp, out Hand? foundHand)
        {
            foreach (var hand in handComp.Hands)
            {
                if (hand.Value.HeldEntity != null && hand.Value.HeldEntity.Value == target)
                {
                    foundHand = hand.Value;
                    return true;
                }
            }

            foundHand = null;
            return false;
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
