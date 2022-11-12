using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class ParticleAcceleratorSystem
    {
        private void InitializePartSystem()
        {
            SubscribeLocalEvent<ParticleAcceleratorPartComponent, MoveEvent>(OnMoveEvent);
            SubscribeLocalEvent<ParticleAcceleratorPartComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ParticleAcceleratorPartComponent component,
            ref PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchorChanged();
        }

        private static void OnMoveEvent(EntityUid uid, ParticleAcceleratorPartComponent component, ref MoveEvent args)
        {
            component.Moved();
        }
    }
}
