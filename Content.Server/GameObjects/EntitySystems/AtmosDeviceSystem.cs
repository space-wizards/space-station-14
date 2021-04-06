using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosDeviceSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AtmosDeviceUpdateEvent>(OnAtmosProcess);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
            UnsubscribeLocalEvent<AtmosDeviceComponent, AtmosDeviceUpdateEvent>(OnAtmosProcess);
        }

        private void OnAtmosProcess(EntityUid uid, AtmosDeviceComponent component, AtmosDeviceUpdateEvent _)
        {
            if (component.Atmosphere == null)
                return; // Shouldn't really happen, but just in case...

            foreach (var process in ComponentManager.GetComponents<IAtmosProcess>(uid))
            {
                process.ProcessAtmos(component.Atmosphere);
            }
        }

        private void OnBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
            if (args.Anchored)
                component.JoinAtmosphere();
            else
                component.LeaveAtmosphere();
        }
    }
}
