using Content.Server.GameObjects.Components.Atmos.Piping;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.EntitySystems
{
    public class AtmosDeviceSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
        }

        private void OnBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
        }
    }
}
