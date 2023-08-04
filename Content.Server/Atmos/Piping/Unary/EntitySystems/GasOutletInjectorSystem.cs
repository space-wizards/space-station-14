using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasOutletInjectorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasOutletInjectorComponent, AtmosDeviceUpdateEvent>(OnOutletInjectorUpdated);
            SubscribeLocalEvent<GasOutletInjectorComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<GasOutletInjectorComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, GasOutletInjectorComponent component, MapInitEvent args)
        {
            UpdateAppearance(uid, component);
        }

        private void OnActivate(EntityUid uid, GasOutletInjectorComponent component, ActivateInWorldEvent args)
        {
            component.Enabled = !component.Enabled;
            UpdateAppearance(uid, component);
        }

        public void UpdateAppearance(EntityUid uid, GasOutletInjectorComponent component, AppearanceComponent? appearance = null)
        {
            if (!Resolve(component.Owner, ref appearance, false))
                return;

            _appearance.SetData(uid, OutletInjectorVisuals.Enabled, component.Enabled, appearance);
        }

        private void OnOutletInjectorUpdated(EntityUid uid, GasOutletInjectorComponent injector, AtmosDeviceUpdateEvent args)
        {
            if (!injector.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            if (!nodeContainer.TryGetNode(injector.InletName, out PipeNode? inlet))
                return;

            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            if (environment == null)
                return;

            if (inlet.Air.Temperature < 0)
                return;

            if (environment.Pressure > injector.MaxPressure)
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            // TODO adjust ratio so that environment does not go above MaxPressure?
            var ratio = MathF.Min(1f, timeDelta * injector.TransferRate / inlet.Air.Volume);
            var removed = inlet.Air.RemoveRatio(ratio);

            _atmosphereSystem.Merge(environment, removed);
        }
    }
}
