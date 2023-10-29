using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Nodes.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPassiveVentSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPassiveVentComponent, AtmosDeviceUpdateEvent>(OnPassiveVentUpdated);
        }

        private void OnPassiveVentUpdated(EntityUid uid, GasPassiveVentComponent vent, AtmosDeviceUpdateEvent args)
        {
            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            if (environment == null)
                return;

            if (!_nodeSystem.TryGetNode<AtmosPipeNodeComponent>((uid, null), vent.InletName, out var inlet) ||
                !_pipeNodeSystem.TryGetGas((inlet.Owner, inlet.Comp2, inlet.Comp1), out var inletGas))
                return;

            var inletAir = inletGas.RemoveRatio(1f);
            var envAir = environment.RemoveRatio(1f);

            var mergeAir = new GasMixture(inletAir.Volume + envAir.Volume);
            _atmosphereSystem.Merge(mergeAir, inletAir);
            _atmosphereSystem.Merge(mergeAir, envAir);

            _atmosphereSystem.Merge(inletGas, mergeAir.RemoveVolume(inletAir.Volume));
            _atmosphereSystem.Merge(environment, mergeAir);
        }
    }
}
