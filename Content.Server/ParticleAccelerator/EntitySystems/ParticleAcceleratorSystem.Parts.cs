using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class ParticleAcceleratorSystem
    {
        private void InitializePartSystem()
        {
            SubscribeLocalEvent<ParticleAcceleratorPartComponent, RotateEvent>(OnRotateEvent);
            SubscribeLocalEvent<ParticleAcceleratorPartComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ParticleAcceleratorPartComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchorChanged();
        }

        private static void OnRotateEvent(EntityUid uid, ParticleAcceleratorPartComponent component, ref RotateEvent args)
        {
            component.Rotated();
        }
    }
}
