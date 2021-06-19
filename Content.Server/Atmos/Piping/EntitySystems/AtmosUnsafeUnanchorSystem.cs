using Content.Server.Anchor;
using Content.Server.Atmos.Piping.Components;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.Atmos;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public class AtmosUnsafeUnanchorSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BeforeUnanchoredEvent>(OnBeforeUnanchored);
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        private void OnUnanchorAttempt(EntityUid uid, AtmosUnsafeUnanchorComponent component, UnanchorAttemptEvent args)
        {
            if (!component.Enabled || !ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (!component.Owner.Transform.Coordinates.TryGetTileAir(out var environment, EntityManager))
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
            if (!component.Enabled || !ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (!component.Owner.Transform.Coordinates.TryGetTileAtmosphere(out var environment))
                environment = null;

            var environmentPressure = environment?.Air?.Pressure ?? 0f;
            var environmentVolume = environment?.Air?.Volume ?? Atmospherics.CellVolume;
            var environmentTemperature = environment?.Air?.Volume ?? Atmospherics.TCMB;

            var lost = 0f;
            var timesLost = 0;

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                var difference = pipe.Air.Pressure - environmentPressure;
                lost += difference * environmentVolume / (environmentTemperature * Atmospherics.R);
                timesLost++;
            }

            var sharedLoss = lost / timesLost;
            var buffer = new GasMixture();

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe) continue;

                buffer.Merge(pipe.Air.Remove(sharedLoss));
            }

            environment?.AssumeAir(buffer);
        }
    }
}
