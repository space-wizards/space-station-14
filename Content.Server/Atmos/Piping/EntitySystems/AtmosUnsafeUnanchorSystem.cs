using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Destructible;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosUnsafeUnanchorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly NodeGroupSystem _group = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UserUnanchoredEvent>(OnUserUnanchored);
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

        // When unanchoring a pipe, leak the gas that was inside the pipe element.
        // At this point the pipe has been scheduled to be removed from the group, but that won't happen until the next Update() call in NodeGroupSystem,
        // so we have to force an update.
        // This way the gas inside other connected pipes stays unchanged, while the removed pipe is completely emptied.
        private void OnUserUnanchored(EntityUid uid, AtmosUnsafeUnanchorComponent component, UserUnanchoredEvent args)
        {
            if (component.Enabled)
            {
                _group.ForceUpdate();
                LeakGas(uid);
            }
        }

        private void OnBreak(EntityUid uid, AtmosUnsafeUnanchorComponent component, BreakageEventArgs args)
        {
            LeakGas(uid, false);
            // Can't use DoActsBehavior["Destruction"] in the same trigger because that would prevent us
            // from leaking. So we make up for this by queueing deletion here.
            QueueDel(uid);
        }

        /// <summary>
        /// Leak gas from the uid's NodeContainer into the tile atmosphere.
        /// Setting removeFromPipe to false will duplicate the gas inside the pipe intead of moving it.
        /// This is needed to properly handle the gas in the pipe getting deleted with the pipe.
        /// </summary>
        public void LeakGas(EntityUid uid, bool removeFromPipe = true)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodes))
                return;

            if (_atmosphere.GetContainingMixture(uid, true, true) is not { } environment)
                environment = GasMixture.SpaceGas;

            var buffer = new GasMixture();

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is not PipeNode pipe)
                    continue;

                if (removeFromPipe)
                    _atmosphere.Merge(buffer, pipe.Air.RemoveVolume(pipe.Volume));
                else
                {
                    var copy = new GasMixture(pipe.Air); //clone, then remove to keep the original untouched
                    _atmosphere.Merge(buffer, copy.RemoveVolume(pipe.Volume));
                }
            }

            _atmosphere.Merge(environment, buffer);
        }
    }
}
