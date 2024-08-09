using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosUnsafeUnanchorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BeforeUnanchoredEvent>(OnBeforeUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BreakageEventArgs>(OnBreak);
        }

        private void OnUnanchorAttempt(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchorAttemptEvent args)
        {
            if (!component.Enabled || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (_atmosphere.GetContainingMixture(uid, true) is not {} environment)
                return;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe)
                    continue;

                if (pipe.Air.Pressure - environment.Pressure > 2 * Atmospherics.OneAtmosphere)
                {
                    args.Delay += 2f;
                    _popup.PopupEntity(Loc.GetString("comp-atmos-unsafe-unanchor-warning"), pipe.Owner,
                        args.User, PopupType.MediumCaution);
                    return; // Show the warning only once.
                }
            }
        }

        private void OnBeforeUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, BeforeUnanchoredEvent args)
        {
            if (component.Enabled)
                LeakGas(uid);
        }

        private void OnBreak(EntityUid uid, AtmosUnsafeUnanchorComponent component, BreakageEventArgs args)
        {
            LeakGas(uid);
            // Can't use DoActsBehavior["Destruction"] in the same trigger because that would prevent us
            // from leaking. So we make up for this by queueing deletion here.
            QueueDel(uid);
        }

        /// <summary>
        /// Leak gas from the uid's NodeContainer into the tile atmosphere.
        /// </summary>
        public void LeakGas(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (_atmosphere.GetContainingMixture(uid, true, true) is not {} environment)
                environment = GasMixture.SpaceGas;

            var lost = 0f;
            var timesLost = 0;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe)
                    continue;

                var difference = pipe.Air.Pressure - environment.Pressure;
                lost += Math.Min(
                            pipe.Volume / pipe.Air.Volume * pipe.Air.TotalMoles,
                            difference * environment.Volume / (environment.Temperature * Atmospherics.R)
                        );
                timesLost++;
            }

            var sharedLoss = lost / timesLost;
            var buffer = new GasMixture();

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe)
                    continue;

                _atmosphere.Merge(buffer, pipe.Air.Remove(sharedLoss));
            }

            _atmosphere.Merge(environment, buffer);
        }
    }
}
