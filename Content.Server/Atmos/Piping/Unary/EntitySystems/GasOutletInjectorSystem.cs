using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasOutletInjectorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

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

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            if (!nodeContainer.TryGetNode(injector.InletName, out PipeNode? inlet))
                return;

            var environment = _atmosphereSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(injector.Owner).Coordinates, true);

            if (environment == null)
                return;

            if (inlet.Air.Temperature < 0)
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;
            var ratio = MathF.Min(1f, timeDelta * injector.TransferRate / inlet.Air.Volume);
            var removed = inlet.Air.RemoveRatio(ratio);

            _atmosphereSystem.Merge(environment, removed);
        }
    }
}
