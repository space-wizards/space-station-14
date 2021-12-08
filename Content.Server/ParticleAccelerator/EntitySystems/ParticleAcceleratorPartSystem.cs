using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    [UsedImplicitly]
    public class ParticleAcceleratorPartSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
            SubscribeLocalEvent<ParticleAcceleratorPartComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ParticleAcceleratorPartComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchorChanged();
        }

        private void RotateEvent(ref RotateEvent ev)
        {
            if (EntityManager.TryGetComponent(ev.Sender, out ParticleAcceleratorPartComponent? part))
            {
                part.Rotated();
            }
        }
    }
}
