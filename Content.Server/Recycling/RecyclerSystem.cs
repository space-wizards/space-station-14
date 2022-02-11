using Content.Server.Power.Components;
using Content.Server.Recycling.Components;
using Content.Shared.Body.Components;
using Content.Shared.Recycling;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Recycling
{
    public sealed class RecyclerSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tags = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(OnCollide);
        }

        private void OnCollide(EntityUid uid, RecyclerComponent component, StartCollideEvent args)
        {
            if (args.OurFixture.ID != "brrt") return;

            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, EntityUid entity)
        {
            RecyclableComponent? recyclable = null;

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!_tags.HasTag(entity, "Recyclable") &&
                (!EntityManager.TryGetComponent(entity, out recyclable) || !recyclable.Safe && component.Safe))
            {
                return;
            }

            // TODO: Prevent collision with recycled items

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                EntityManager.GetComponent<SharedBodyComponent>(entity).Gib(true);
                Bloodstain(component);
                return;
            }

            if (recyclable == null)
                QueueDel(entity);
            else
                Recycle(recyclable, component.Efficiency);
        }

        private bool CanGib(RecyclerComponent component, EntityUid entity)
        {
            // TODO: Power needs a helper for this jeez
            return HasComp<SharedBodyComponent>(entity) && !component.Safe &&
                   TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver) && receiver.Powered;
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }

        private void Recycle(RecyclableComponent component, float efficiency = 1f)
        {
            if (!string.IsNullOrEmpty(component.Prototype))
            {
                var xform = Transform(component.Owner);

                for (var i = 0; i < Math.Max(component.Amount * efficiency, 1); i++)
                {
                    EntityManager.SpawnEntity(component.Prototype, xform.Coordinates);
                }

            }

            EntityManager.QueueDeleteEntity(component.Owner);
        }
    }
}
