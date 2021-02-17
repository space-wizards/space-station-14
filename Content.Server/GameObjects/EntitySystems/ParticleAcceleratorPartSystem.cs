#nullable enable
using Content.Server.GameObjects.Components.PA;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ParticleAcceleratorPartSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
        }

        private static void RotateEvent(RotateEvent ev)
        {
            if (ev.Sender.TryGetComponent(out ParticleAcceleratorPartComponent? part))
            {
                part.Rotated();
            }
        }
    }
}
