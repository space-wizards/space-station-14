using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosUnsafeUnanchorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BeforeUnanchoredEvent>(OnBeforeUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        private void OnUnanchorAttempt(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchorAttemptEvent args)
        {
            if (!component.Enabled)
                return;

            if (_atmosphere.GetContainingMixture(uid, true) is not { } environment)
                return;

            foreach (var (nodeId, node) in _nodeSystem.EnumerateNodes(uid))
            {
                if (!TryComp<AtmosPipeNodeComponent>(nodeId, out var pipe) ||
                    !_pipeNodeSystem.TryGetGas((nodeId, pipe, node), out var nodeGas))
                    continue;

                if (nodeGas.Pressure - environment.Pressure < 2 * Atmospherics.OneAtmosphere)
                    continue;

                args.Delay += 2f;
                _popup.PopupEntity(
                    Loc.GetString("comp-atmos-unsafe-unanchor-warning"),
                    uid,
                    args.User, PopupType.MediumCaution
                );
                return; // Show the warning only once.
            }
        }

        private void OnBeforeUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, BeforeUnanchoredEvent args)
        {
            if (!component.Enabled)
                return;

            if (_atmosphere.GetContainingMixture(uid, true, true) is not { } environment)
                environment = GasMixture.SpaceGas;

            var lost = 0f;
            var timesLost = 0;
            foreach (var (nodeId, node) in _nodeSystem.EnumerateNodes(uid))
            {
                if (!TryComp<AtmosPipeNodeComponent>(nodeId, out var pipeNode) ||
                    !_pipeNodeSystem.TryGetGas((nodeId, pipeNode, node), out var nodeGas))
                    continue;

                var difference = nodeGas.Pressure - environment.Pressure;
                lost += difference * environment.Volume / (environment.Temperature * Atmospherics.R);
                timesLost++;
            }

            var sharedLoss = lost / timesLost;
            foreach (var (nodeId, node) in _nodeSystem.EnumerateNodes(uid))
            {
                if (!TryComp<AtmosPipeNodeComponent>(nodeId, out var pipeNode) ||
                    !_pipeNodeSystem.TryGetGas((nodeId, pipeNode, node), out var nodeGas))
                    continue;

                _atmosphere.Merge(environment, nodeGas.Remove(sharedLoss));
            }
        }
    }
}
