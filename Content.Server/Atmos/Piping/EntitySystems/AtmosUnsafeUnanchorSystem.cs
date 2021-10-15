using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Construction.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public class AtmosUnsafeUnanchorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BeforeUnanchoredEvent>(OnBeforeUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        private void OnUnanchorAttempt(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchorAttemptEvent args)
        {
            if (!component.Enabled || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates) is not {} environment)
                return;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                if ((pipe.Air.Pressure - environment.Pressure) > 2 * Atmospherics.OneAtmosphere)
                {
                    args.Delay += 1.5f;
                    args.User?.PopupMessageCursor(Loc.GetString("comp-atmos-unsafe-unanchor-warning"));
                    return; // Show the warning only once.
                }
            }
        }

        private void OnBeforeUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, BeforeUnanchoredEvent args)
        {
            if (!component.Enabled || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates, true) is not {} environment)
                environment = GasMixture.SpaceGas;

            var lost = 0f;
            var timesLost = 0;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                var difference = pipe.Air.Pressure - environment.Pressure;
                lost += difference * environment.Volume / (environment.Temperature * Atmospherics.R);
                timesLost++;
            }

            var sharedLoss = lost / timesLost;
            var buffer = new GasMixture();

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                _atmosphereSystem.Merge(buffer, pipe.Air.Remove(sharedLoss));
            }

            _atmosphereSystem.Merge(environment, buffer);
        }
    }
}
