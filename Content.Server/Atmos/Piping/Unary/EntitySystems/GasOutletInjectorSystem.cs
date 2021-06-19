using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasOutletInjectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasOutletInjectorComponent, AtmosDeviceUpdateEvent>(OnOutletInjectorUpdated);
        }

        private void OnOutletInjectorUpdated(EntityUid uid, GasOutletInjectorComponent injector, AtmosDeviceUpdateEvent args)
        {
            injector.Injecting = false;

            if (!injector.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(injector.InletName, out PipeNode? inlet))
                return;

            var environment = args.Atmosphere.GetTile(injector.Owner.Transform.Coordinates)!;

            if (environment.Air == null)
                return;

            if (inlet.Air.Temperature > 0)
            {
                var transferMoles = inlet.Air.Pressure * injector.VolumeRate / (inlet.Air.Temperature * Atmospherics.R);

                var removed = inlet.Air.Remove(transferMoles);

                environment.AssumeAir(removed);
                environment.Invalidate();
            }
        }
    }
}
