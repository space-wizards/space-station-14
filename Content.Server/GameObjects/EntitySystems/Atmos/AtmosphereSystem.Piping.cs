using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Binary;
using Content.Server.GameObjects.Components.Atmos.Piping.Other;
using Content.Server.GameObjects.Components.Atmos.Piping.Trinary;
using Content.Server.GameObjects.Components.Atmos.Piping.Unary;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    public partial class AtmosphereSystem
    {
        private void InitializePiping()
        {
            InitializeUnary();
            InitializeBinary();
            InitializeTrinary();
            InitializeOther();
        }

        #region Unary
        private void InitializeUnary()
        {
            SubscribeLocalEvent<GasVentComponent, AtmosDeviceUpdateEvent>(OnGasVentUpdated);
            SubscribeLocalEvent<GasScrubberComponent, AtmosDeviceUpdateEvent>(OnScrubberUpdated);
            SubscribeLocalEvent<GasPassiveVentComponent, AtmosDeviceUpdateEvent>(OnPassiveVentUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasOutletInjectorComponent, AtmosDeviceUpdateEvent>(OnOutletInjectorUpdated);

            SubscribeLocalEvent<GasTankComponent, ComponentStartup>(OnTankStartup);

            // Portable Atmospherics.
            SubscribeLocalEvent<GasPortableComponent, AnchorAttemptEvent>(OnPortableAnchorAttempt);
            SubscribeLocalEvent<GasPortableComponent, AnchoredEvent>(OnPortableAnchored);
            SubscribeLocalEvent<GasPortableComponent, UnanchoredEvent>(OnPortableUnanchored);
        }

        private void OnGasVentUpdated(EntityUid uid, GasVentComponent vent, AtmosDeviceUpdateEvent args)
        {
            // TODO ATMOS: Weld shut.
            if (!vent.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? pipe))
                return;

            var environment = args.Atmosphere.GetTile(vent.Owner.Transform.Coordinates)!;

            // We're in an air-blocked tile... Do nothing.
            if (environment.Air == null)
                return;

            if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                var pressureDelta = 10000f;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.ExternalPressureBound - environment.Air.Pressure);

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, pipe.Air.Pressure - vent.InternalPressureBound);

                if (pressureDelta > 0 && pipe.Air.Temperature > 0)
                {
                    var transferMoles = pressureDelta * environment.Air.Volume / (pipe.Air.Temperature * Atmospherics.R);

                    environment.AssumeAir(pipe.Air.Remove(transferMoles));
                }
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Air.Pressure > 0)
            {
                var ourMultiplier = pipe.Air.Volume / (environment.Air.Temperature * Atmospherics.R);
                var molesDelta = 10000f * ourMultiplier;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta,
                        (environment.Air.Pressure - vent.ExternalPressureBound) * environment.Air.Volume /
                        (environment.Air.Temperature * Atmospherics.R));

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta, (vent.InternalPressureBound - pipe.Air.Pressure) * ourMultiplier);

                if (molesDelta > 0)
                {
                    var removed = environment.Air.Remove(molesDelta);
                    pipe.Air.Merge(removed);
                    environment.Invalidate();
                }
            }
        }

        private void OnScrubberUpdated(EntityUid uid, GasScrubberComponent scrubber, AtmosDeviceUpdateEvent args)
        {
            if (!scrubber.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet))
                return;

            var environment = args.Atmosphere.GetTile(scrubber.Owner.Transform.Coordinates)!;

            Scrub(scrubber, environment, outlet);

            if (!scrubber.WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in environment.AdjacentTiles)
            {
                Scrub(scrubber, adjacent, outlet);
            }
        }

        private void OnPassiveVentUpdated(EntityUid uid, GasPassiveVentComponent vent, AtmosDeviceUpdateEvent args)
        {
            var environment = args.Atmosphere.GetTile(vent.Owner.Transform.Coordinates)!;

            if (environment.Air == null)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(vent.InletName, out PipeNode? inlet))
                return;

            var environmentPressure = environment.Air.Pressure;
            var pressureDelta = MathF.Abs(environmentPressure - inlet.Air.Pressure);

            if ((environment.Air.Temperature > 0 || inlet.Air.Temperature > 0) && pressureDelta > 0.5f)
            {
                if (environmentPressure < inlet.Air.Pressure)
                {
                    var airTemperature = environment.Temperature > 0 ? environment.Temperature : inlet.Air.Temperature;
                    var transferMoles = pressureDelta * environment.Air.Volume / (airTemperature * Atmospherics.R);
                    var removed = inlet.Air.Remove(transferMoles);
                    environment.AssumeAir(removed);
                }
                else
                {
                    var airTemperature = inlet.Air.Temperature > 0 ? inlet.Air.Temperature : environment.Temperature;
                    var outputVolume = inlet.Air.Volume;
                    var transferMoles = (pressureDelta * outputVolume) / (airTemperature * Atmospherics.R);
                    transferMoles = MathF.Min(transferMoles, environment.Air.TotalMoles * inlet.Air.Volume / environment.Air.Volume);
                    var removed = environment.Air.Remove(transferMoles);
                    inlet.Air.Merge(removed);
                    environment.Invalidate();
                }
            }

        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {
            if (!thermoMachine.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(thermoMachine.InletName, out PipeNode? inlet))
                return;

            var airHeatCapacity = inlet.Air.HeatCapacity;
            var combinedHeatCapacity = airHeatCapacity + thermoMachine.HeatCapacity;
            var oldTemperature = inlet.Air.Temperature;

            if (combinedHeatCapacity > 0)
            {
                var combinedEnergy = thermoMachine.HeatCapacity * thermoMachine.TargetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
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

        private void OnTankStartup(EntityUid uid, GasTankComponent tank, ComponentStartup args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(tank.TankName, out PipeNode? tankNode))
                return;

            // Create a pipenet if we don't have one already.
            tankNode.TryAssignGroupIfNeeded();
            tankNode.Air.Merge(tank.InitialMixture);
            tankNode.Air.Temperature = tank.InitialMixture.Temperature;
        }

        private void OnPortableAnchorAttempt(EntityUid uid, GasPortableComponent component, AnchorAttemptEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out ITransformComponent? transform))
                return;

            // If we can't find any ports, cancel the anchoring.
            if(!FindGasPortIn(transform.GridID, transform.Coordinates, out _))
                args.Cancel();
        }

        private void OnPortableAnchored(EntityUid uid, GasPortableComponent portable, AnchoredEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = true;

            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(GasPortableVisuals.ConnectedState, true);
            }
        }

        private void OnPortableUnanchored(EntityUid uid, GasPortableComponent portable, UnanchoredEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = false;

            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(GasPortableVisuals.ConnectedState, false);
            }
        }

        #endregion

        #region Binary
        private void InitializeBinary()
        {
            SubscribeLocalEvent<GasPumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
            SubscribeLocalEvent<GasPortComponent, AtmosDeviceUpdateEvent>(OnPortUpdated);
            SubscribeLocalEvent<GasCanisterComponent, AtmosDeviceUpdateEvent>(OnCanisterUpdated);
            SubscribeLocalEvent<GasVolumePumpComponent, AtmosDeviceUpdateEvent>(OnVolumePumpUpdated);
            SubscribeLocalEvent<GasPassiveGateComponent, AtmosDeviceUpdateEvent>(OnPassiveGateUpdated);

            SubscribeLocalEvent<GasCanisterComponent, ComponentStartup>(OnCanisterStartup);
            SubscribeLocalEvent<GasCanisterComponent, InteractUsingEvent>(OnCanisterInteractUsing);
            SubscribeLocalEvent<GasCanisterComponent, EntInsertedIntoContainerMessage>(OnCanisterContainerInserted);
            SubscribeLocalEvent<GasCanisterComponent, EntRemovedFromContainerMessage>(OnCanisterContainerRemoved);
        }

        private void OnCanisterInteractUsing(EntityUid uid, GasCanisterComponent component, InteractUsingEvent args)
        {
            var canister = EntityManager.GetEntity(uid);
            var container = canister.EnsureContainer<ContainerSlot>(component.ContainerName);

            // Container full.
            if (container.ContainedEntity != null)
                return;

            // TODO: Make entire codebase use ECS so we can kill these checks below and use events instead.
            // Check the used item is valid...
            if (!args.Used.TryGetComponent(out GasTankComponent? tank)
                || !args.Used.TryGetComponent(out NodeContainerComponent? tankNodeContainer))
                return;

            // Check the user has hands.
            if (!args.User.TryGetComponent(out HandsComponent? hands))
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!args.User.InRangeUnobstructed(canister, 1f, popup: true))
                return;

            if (!tankNodeContainer.TryGetNode(tank.TankName, out PipeNode? tankNode))
                return;

            if (!nodeContainer.TryGetNode(component.TankName, out PipeNode? canisterNode))
                return;

            if (!hands.Drop(args.Used, canister.Transform.Coordinates))
                return;

            if (!container.Insert(args.Used))
                return;

            canisterNode.NodeGroup.AddNode(tankNode);

            args.Handled = true;
        }

        private void OnCanisterContainerInserted(EntityUid uid, GasCanisterComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, true);
        }

        private void OnCanisterContainerRemoved(EntityUid uid, GasCanisterComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, false);
        }

        private void OnPumpUpdated(EntityUid uid, GasPumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;

            if (MathHelper.CloseTo(pump.TargetPressure, outputStartingPressure))
                return; // No need to pump gas if target has been reached.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = pump.TargetPressure - outputStartingPressure;
                var transferMoles = pressureDelta * outlet.Air.Volume / inlet.Air.Temperature * Atmospherics.R;

                var removed = inlet.Air.Remove(transferMoles);
                outlet.Air.Merge(removed);
            }
        }

        private void OnPortUpdated(EntityUid uid, GasPortComponent port, AtmosDeviceUpdateEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(port.PipeName, out PipeNode? pipe)
                || !nodeContainer.TryGetNode(port.ConnectedName, out PipeNode? connected))
                return;

            // Clear before use, always!
            port.Buffer.Clear();
            port.Buffer.Volume = pipe.Air.Volume + connected.Air.Volume;

            port.Buffer.Merge(pipe.Air);
            port.Buffer.Merge(connected.Air);

            pipe.Air.Clear();
            pipe.Air.Merge(port.Buffer);
            pipe.Air.Multiply(pipe.Air.Volume / port.Buffer.Volume);

            connected.Air.Clear();
            connected.Air.Merge(port.Buffer);
            connected.Air.Multiply(connected.Air.Volume / port.Buffer.Volume);
        }

        private void OnCanisterUpdated(EntityUid uid, GasCanisterComponent canister, AtmosDeviceUpdateEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            ||  !ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode))
                return;

            // Nothing to do here.
            if (MathHelper.CloseTo(portNode.Air.Pressure, canister.LastPressure))
                return;

            canister.LastPressure = portNode.Air.Pressure;

            if (portNode.Air.Pressure < 10)
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 0);
            }
            else if (portNode.Air.Pressure < Atmospherics.OneAtmosphere)
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 1);
            }
            else if (portNode.Air.Pressure < (15 * Atmospherics.OneAtmosphere))
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 2);
            }
            else
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 3);
            }
        }


        private void OnVolumePumpUpdated(EntityUid uid, GasVolumePumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
                return;

            var inputStartingPressure = inlet.Air.Pressure;
            var outputStartingPressure = outlet.Air.Pressure;

            // Pump mechanism won't do anything if the pressure is too high/too low unless you overclock it.
            if ((inputStartingPressure < 0.01f) || (outputStartingPressure > 9000) && !pump.Overclocked)
                return;

            // Overclocked pumps can only force gas a certain amount.
            if ((outputStartingPressure - inputStartingPressure > 1000) && pump.Overclocked)
                return;

            var transferRatio = pump.TransferRate / inlet.Air.Volume;

            var removed = inlet.Air.RemoveRatio(transferRatio);

            // Some of the gas from the mixture leaks when overclocked.
            if (pump.Overclocked)
            {
                var tile = args.Atmosphere.GetTile(pump.Owner.Transform.Coordinates);

                if (tile != null)
                {
                    var leaked = removed.RemoveRatio(pump.LeakRatio);
                    tile.AssumeAir(leaked);
                }
            }

            outlet.Air.Merge(removed);
        }

        private void OnPassiveGateUpdated(EntityUid uid, GasPassiveGateComponent gate, AtmosDeviceUpdateEvent args)
        {
            if (!gate.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(gate.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(gate.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;
            var inputStartingPressure = inlet.Air.Pressure;

            if (outputStartingPressure >= MathF.Min(gate.TargetPressure, inputStartingPressure - gate.FrictionPressureDifference))
                return; // No need to pump gas, target reached or input pressure too low.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = MathF.Min(gate.TargetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure)/2);
                // We can't have a pressure delta that would cause outlet pressure > inlet pressure.

                var transferMoles = pressureDelta * outlet.Air.Volume / (inlet.Air.Temperature * Atmospherics.R);

                // Actually transfer the gas.
                outlet.Air.Merge(inlet.Air.Remove(transferMoles));
            }
        }

        private void OnCanisterStartup(EntityUid uid, GasCanisterComponent canister, ComponentStartup args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode))
                return;

            // Create a pipenet if we don't have one already.
            portNode.TryAssignGroupIfNeeded();
            portNode.Air.Merge(canister.InitialMixture);
            portNode.Air.Temperature = canister.InitialMixture.Temperature;
        }

        #endregion

        #region Trinary
        private void InitializeTrinary()
        {
            SubscribeLocalEvent<GasMixerComponent, AtmosDeviceUpdateEvent>(OnMixerUpdated);
            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
        }

        private void OnMixerUpdated(EntityUid uid, GasMixerComponent mixer, AtmosDeviceUpdateEvent args)
        {
            // TODO ATMOS: Cache total moles since it's expensive.

            if (!mixer.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(mixer.InletOneName, out PipeNode? inletOne)
                || !nodeContainer.TryGetNode(mixer.InletTwoName, out PipeNode? inletTwo)
                || !nodeContainer.TryGetNode(mixer.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;

            if (outputStartingPressure >= mixer.TargetPressure)
                return; // Target reached, no need to mix.

            var generalTransfer = (mixer.TargetPressure - outputStartingPressure) * outlet.Air.Volume / Atmospherics.R;

            var transferMolesOne = inletOne.Air.Temperature > 0 ? mixer.InletOneConcentration * generalTransfer / inletOne.Air.Temperature : 0f;
            var transferMolesTwo = inletTwo.Air.Temperature > 0 ? mixer.InletTwoConcentration * generalTransfer / inletTwo.Air.Temperature : 0f;

            if (mixer.InletTwoConcentration <= 0f)
            {
                if (inletOne.Air.Temperature <= 0f)
                    return;

                transferMolesOne = MathF.Min(transferMolesOne, inletOne.Air.TotalMoles);
                transferMolesTwo = 0f;
            }

            else if (mixer.InletOneConcentration <= 0)
            {
                if (inletTwo.Air.Temperature <= 0f)
                    return;

                transferMolesOne = 0f;
                transferMolesTwo = MathF.Min(transferMolesTwo, inletTwo.Air.TotalMoles);
            }
            else
            {
                if (inletOne.Air.Temperature <= 0f || inletTwo.Air.Temperature <= 0f)
                    return;

                if (transferMolesOne <= 0 || transferMolesTwo <= 0)
                    return;

                if (inletOne.Air.TotalMoles < transferMolesOne || inletTwo.Air.TotalMoles < transferMolesTwo)
                {
                    var ratio = MathF.Min(inletOne.Air.TotalMoles / transferMolesOne, inletTwo.Air.TotalMoles / transferMolesTwo);
                    transferMolesOne *= ratio;
                    transferMolesTwo *= ratio;
                }
            }

            // Actually transfer the gas now.

            if (transferMolesOne > 0f)
            {
                var removed = inletOne.Air.Remove(transferMolesOne);
                outlet.Air.Merge(removed);
            }

            if (transferMolesTwo > 0f)
            {
                var removed = inletTwo.Air.Remove(transferMolesTwo);
                outlet.Air.Merge(removed);
            }
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, AtmosDeviceUpdateEvent args)
        {
            if (!filter.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(filter.InletName, out PipeNode? inletNode)
                || !nodeContainer.TryGetNode(filter.FilterName, out PipeNode? filterNode)
                || !nodeContainer.TryGetNode(filter.OutletName, out PipeNode? outletNode))
                return;

            if (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure)
                return; // No need to transfer if target is full.

            // SUS: Maybe this should take time into account, transfer rate is L/s...
            var transferRatio = filter.TransferRate / inletNode.Air.Volume;

            if (transferRatio <= 0)
                return;

            var removed = inletNode.Air.RemoveRatio(transferRatio);

            if (filter.FilteredGas.HasValue)
            {
                var filteredOut = new GasMixture(this) {Temperature = removed.Temperature};

                filteredOut.SetMoles(filter.FilteredGas.Value, removed.GetMoles(filter.FilteredGas.Value));
                removed.SetMoles(filter.FilteredGas.Value, 0f);

                var target = filterNode.Air.Pressure < Atmospherics.MaxOutputPressure ? filterNode.Air : inletNode.Air;
                target.Merge(filteredOut);
            }

            outletNode.Air.Merge(removed);
        }

        #endregion

        #region Other
        private void InitializeOther()
        {
            SubscribeLocalEvent<GasMinerComponent, AtmosDeviceUpdateEvent>(OnMinerUpdated);
        }

        private void OnMinerUpdated(EntityUid uid, GasMinerComponent miner, AtmosDeviceUpdateEvent args)
        {
            if (!CheckMinerOperation(args.Atmosphere, miner, out var tile) || !miner.Enabled || miner.SpawnGas <= Gas.Invalid || miner.SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1, this) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas, miner.SpawnAmount);

            tile.AssumeAir(merger);
        }

        #endregion

        #region Helpers

        private void Scrub(GasScrubberComponent scrubber, TileAtmosphere? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile?.Air == null)
                return;

            // Cannot scrub if pressure too high.
            if (outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere)
                return;

            if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                var transferMoles = MathF.Min(1f, (scrubber.VolumeRate / tile.Air.Volume) * tile.Air.TotalMoles);

                // Take a gas sample.
                var removed = tile.Air.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseTo(removed.TotalMoles, 0f))
                    return;

                removed.ScrubInto(outlet.Air, scrubber.FilterGases);

                // Remix the gases.
                tile.AssumeAir(removed);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                var transferMoles = tile.Air.TotalMoles * (scrubber.VolumeRate / tile.Air.Volume);

                var removed = tile.Air.Remove(transferMoles);

                outlet.Air.Merge(removed);
                tile.Invalidate();
            }
        }

        private bool CheckMinerOperation(IGridAtmosphereComponent atmosphere, GasMinerComponent miner, [NotNullWhen(true)] out TileAtmosphere? tile)
        {
            tile = atmosphere.GetTile(miner.Owner.Transform.Coordinates)!;

            // Space.
            if (atmosphere.IsSpace(tile.GridIndices))
            {
                miner.Broken = true;
                return false;
            }

            // Airblocked location.
            if (tile.Air == null)
            {
                miner.Broken = true;
                return false;
            }

            // External pressure above threshold.
            if (!float.IsInfinity(miner.MaxExternalPressure) &&
                tile.Air.Pressure > miner.MaxExternalPressure - miner.SpawnAmount * miner.SpawnTemperature * Atmospherics.R / tile.Air.Volume)
            {
                miner.Broken = true;
                return false;
            }

            // External gas amount above threshold.
            if (!float.IsInfinity(miner.MaxExternalAmount) && tile.Air.TotalMoles > miner.MaxExternalAmount)
            {
                miner.Broken = true;
                return false;
            }

            miner.Broken = false;
            return true;
        }

        private bool FindGasPortIn(GridId gridId, EntityCoordinates coordinates, [NotNullWhen(true)] out GasPortComponent? port)
        {
            port = null;

            if (!gridId.IsValid())
                return false;

            var grid = _mapManager.GetGrid(gridId);

            foreach (var entityUid in grid.GetLocal(coordinates))
            {
                if (ComponentManager.TryGetComponent<GasPortComponent>(entityUid, out port))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
