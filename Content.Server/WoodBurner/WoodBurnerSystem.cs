using System.Threading;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Atmos.Portable;
using Content.Server.Construction.Components;
using Content.Server.Construction;
using Content.Server.Stack;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.WoodBurner;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;


namespace Content.Server.WoodBurner
{
    public sealed class WoodBurnerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        //[Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        //[Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            //SubscribeLocalEvent<ActiveWoodBurnerComponent, ComponentInit>(OnInit);
            //SubscribeLocalEvent<ActiveWoodBurnerComponent, ComponentShutdown>(OnShutdown);


            //SubscribeLocalEvent<SharedWoodBurnerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<WoodBurnerComponent,AtmosDeviceDisabledEvent>(OnMachineLeaveAtmosphere);
            SubscribeLocalEvent<WoodBurnerComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<WoodBurnerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        }

        /*
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, burner) in EntityQuery<ActiveWoodBurnerComponent, WoodBurnerComponent>())
            {
                burner.ProcessingTimer -= frameTime;
                //reclaimer.RandomMessTimer -= frameTime;
            }
        }
        */

        /*
        public void OnInit()
        {

        }
        
        public void OnShutdown()
        {

        }
        */


        private void OnAnchorChanged(EntityUid uid, WoodBurnerComponent component, ref AnchorStateChangedEvent args)
        {
            if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(component.InletName, out PipeNode? node))
                return;

            //node.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));
        }

        private void OnAtmosUpdate(EntityUid uid, WoodBurnerComponent component, AtmosDeviceUpdateEvent args)
        {
            /*
            if (WoodBurnerComponent.Enabled)
            {
                if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
                    return;

                if (!nodeContainer.TryGetNode(component.InletName, out PipeNode? node))
                    return;

                //node.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));

                //UpdateDrainingAppearance(uid, portableNode.ConnectionsEnabled);
                var merger = new GasMixture(1) { Temperature = outputGasTemperature };
                merger.SetMoles(Gas.CarbonDioxide, outputGasAmount);
                _atmosphereSystem.Merge(environment, merger);
            }
            */

            if (!component.Enabled
                || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(component.InletName, out PipeNode? inlet))
            {
                /*
                DirtyUI(uid, thermoMachine);
                _appearance.SetData(uid, ThermoMachineVisuals.Enabled, false);
                return;
                */
                return;
            }

            var merger = new GasMixture(1) { Temperature = component.OutputGasTemperature };
            merger.SetMoles(Gas.CarbonDioxide, component.OutputGasAmount);
            _atmosphereSystem.Merge(inlet.Air, merger);

        }

        private void OnMachineLeaveAtmosphere(EntityUid uid, SharedWoodBurnerComponent component, AtmosDeviceDisabledEvent args)
        {

        }

        /*
        private void OnToggleMessage(EntityUid uid, WoodBurnerComponent component, GasThermomachineToggleMessage args)
        {
            component.Enabled = !component.Enabled;

            DirtyUI(uid, component);
        }
        */

        private void OnRefreshParts(EntityUid uid, SharedWoodBurnerComponent component, RefreshPartsEvent args)
        {

        }

    }
}
