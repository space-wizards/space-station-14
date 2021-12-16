using Content.Server.Power.Components;
using Content.Server.Recycling.Components;
using Content.Shared.Body.Components;
using Content.Shared.Recycling;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Recycling
{
    internal sealed class RecyclerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, RecyclerComponent component, StartCollideEvent args)
        {
            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, EntityUid entity)
        {
            // TODO: Prevent collision with recycled items

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!EntityManager.TryGetComponent(entity, out RecyclableComponent? recyclable) || !recyclable.Safe && component.Safe) return;

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                EntityManager.GetComponent<SharedBodyComponent>(entity).Gib(true);
                Bloodstain(component);
                return;
            }

            recyclable.Recycle(component.Efficiency);
        }

        private bool CanGib(RecyclerComponent component, EntityUid entity)
        {
            // We suppose this entity has a Recyclable component.
            return EntityManager.HasComponent<SharedBodyComponent>(entity) && !component.Safe &&
                   EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver) && receiver.Powered;
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }
    }
}
