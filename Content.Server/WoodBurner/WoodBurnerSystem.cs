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
using Content.Server.Materials;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Shared.WoodBurner;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System;
using System.Security.Cryptography;
using System.Diagnostics;


namespace Content.Server.WoodBurner
{
    public sealed class WoodBurnerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        //[Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        //[Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WoodBurnerComponent, ComponentInit>(OnInit);
            //SubscribeLocalEvent<ActiveWoodBurnerComponent, ComponentShutdown>(OnShutdown);


            //SubscribeLocalEvent<SharedWoodBurnerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            //SubscribeLocalEvent<WoodBurnerComponent, MaterialEntityInsertedEvent>(OnWoodInserted);
            SubscribeLocalEvent<WoodBurnerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<WoodBurnerComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<WoodBurnerComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<WoodBurnerComponent, MachineDeconstructedEvent>(OnDeconstruct);
            SubscribeLocalEvent<WoodBurnerComponent, AtmosDeviceDisabledEvent>(OnMachineLeaveAtmosphere);
            SubscribeLocalEvent<WoodBurnerComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<WoodBurnerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        }


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, burner) in EntityQuery<ActiveWoodBurnerComponent, WoodBurnerComponent>())
            {
                burner.ProcessingTimer -= frameTime;
                if (burner.ProcessingTimer <= 0)
                {
                    // Production Gas
                    if (!EntityManager.TryGetComponent(burner.Owner, out NodeContainerComponent? nodeContainer)
                        || !nodeContainer.TryGetNode(burner.InletName, out PipeNode? inlet))
                    {
                        /*
                        DirtyUI(uid, Machine);
                        _appearance.SetData(uid, MachineVisuals.Enabled, false);
                        return;
                        */
                        return;
                    }

                    var merger = new GasMixture(1) { Temperature = burner.OutputGasTemperature };
                    merger.SetMoles(Gas.CarbonDioxide, burner.OutputGasAmount);
                    _atmosphereSystem.Merge(inlet.Air, merger);

                    // Proccesing Materials
                    if (!EntityManager.TryGetComponent(burner.Owner, out MaterialStorageComponent? storage))
                        return;

                    var woodBaseAmount = 100;
                    var charcoalBaseAmount = 100;
                    var materialExchangeRate = (int) Math.Max(woodBaseAmount + (50 * burner.MaterialUseMultiplier), charcoalBaseAmount);

                    if (_material.TryChangeMaterialAmount(burner.Owner, "Wood", -materialExchangeRate, storage))
                    {
                        _material.TryChangeMaterialAmount(burner.Owner, "Charcoal", charcoalBaseAmount, storage);
                        if (_material.GetMaterialAmount(burner.Owner, "Charcoal") >= 3000)
                        {
                            _material.TryChangeMaterialAmount(burner.Owner, "Charcoal", -3000, storage);

                            // TO-DO - Uncomment this after Stack Refactor
                            //_stackSystem.SpawnMultiple(1,100,"CharCoal", Transform(burner.Owner).Coordinates);

                            // TO-DO - Delete this after Stack Refactor
                            var entity = Spawn("MaterialCharCoal", Transform(burner.Owner).Coordinates);
                            var stack = Comp<StackComponent>(entity);
                            _stackSystem.SetCount(entity, 30, stack);

                        }

                        if (_material.GetMaterialAmount(burner.Owner, "Wood") < materialExchangeRate)
                        {
                            TurnOffMachine(burner);
                        }
                        else
                        {
                            burner.ProcessingTimer = burner.ProcessingTimerMax;
                        }
                    }
                    else
                    {
                        TurnOffMachine(burner);
                    }
                }
            }
        }


        public void OnInit(EntityUid uid, WoodBurnerComponent component, ComponentInit args)
        {
            UpdateAppearance(uid, component);
        }
        /*
        public void OnShutdown()
        {

        }
        */

        private void UpdateAppearance(EntityUid uid, WoodBurnerComponent component)
        {
            //_appearance.SetData(uid, WoodBurnerMachineVisuals.Enabled, false);

            _appearance.SetData(uid, WoodBurnerMachineVisuals.Enabled, component.Enabled);
        }

        private void TurnOnMachine(WoodBurnerComponent component)
        {
            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var power) && !power.Powered)
                return;

            if (_material.GetMaterialAmount(component.Owner, "Wood") > 10)
            {
                component.Enabled = true;
                EnsureComp<ActiveWoodBurnerComponent>(component.Owner);
                UpdateAppearance(component.Owner, component);
            }
        }

        private void TurnOffMachine(WoodBurnerComponent component)
        {
            component.Enabled = false;
            RemComp<ActiveWoodBurnerComponent>(component.Owner);
            UpdateAppearance(component.Owner, component);
        }

        private void OnInteractHand(EntityUid uid, WoodBurnerComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = true;

            if (!component.Enabled)
                TurnOnMachine(component);
            else
                TurnOffMachine(component);
        }

        private void OnInteractUsing(EntityUid uid, WoodBurnerComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            if (TryComp<MaterialStorageComponent>(component.Owner, out var storage))
            {
                if (!TryComp<MaterialComponent>(args.Used, out var material))
                    return;
                foreach (var mat in material.Materials)
                {
                    if (mat.ID == "Charcoal")
                        return;
                }
                args.Handled = _material.TryInsertMaterialEntity(args.User, args.Used, uid, storage);
            }
        }

        private void OnPowerChanged(EntityUid uid, WoodBurnerComponent component, ref PowerChangedEvent args)
        {
            if (args.Powered)
            {
                if (component.Enabled)
                    EnsureComp<ActiveWoodBurnerComponent>(uid);
            }
            else
            {
                RemComp<ActiveWoodBurnerComponent>(uid);
            }
            UpdateAppearance(uid, component);
        }

        private void OnDeconstruct(EntityUid uid, WoodBurnerComponent component, MachineDeconstructedEvent args)
        {
            // TO-DO - Uncomment this after StackSystem Refactor

            //_stackSystem.SpawnMultiple(_material.GetMaterialAmount(uid, "Wood"), 100, "Wood", Transform(uid).Coordinates);
            //_stackSystem.SpawnMultiple(_material.GetMaterialAmount(uid, "Charcoal"), 100, "Charcoal", Transform(uid).Coordinates);
        }

        private void OnAnchorChanged(EntityUid uid, WoodBurnerComponent component, ref AnchorStateChangedEvent args)
        {
            if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(component.InletName, out PipeNode? node))
                return;

            //node.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));
        }

        /*
        private void OnWoodInserted(EntityUid uid, WoodBurnerComponent component, MaterialEntityInsertedEvent args)
        {

            //UpdateInsertingAppearance(uid, true, matProto?.Color);
        }
        */

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

            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            if (environment == null)
                return;

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

            /*
            var merger = new GasMixture(1) { Temperature = component.OutputGasTemperature };
            merger.SetMoles(Gas.CarbonDioxide, component.OutputGasAmount);
            _atmosphereSystem.Merge(inlet.Air, merger);
            */

            /*
            // Transfer from inlet to environment
            var environmentPressure = environment.Pressure;
            var pressureDelta = MathF.Abs(environmentPressure - inlet.Air.Pressure);
            if ((environment.Temperature > 0 || inlet.Air.Temperature > 0) && pressureDelta > 0.5f)
            {
                if (environmentPressure < inlet.Air.Pressure)
                {
                    var airTemperature = environment.Temperature > 0 ? environment.Temperature : inlet.Air.Temperature;
                    var transferMoles = pressureDelta * environment.Volume / (airTemperature * Atmospherics.R);
                    var removed = inlet.Air.Remove(transferMoles);
                    _atmosphereSystem.Merge(environment, removed);
                }
            }
            */
        }

        private void OnMachineLeaveAtmosphere(EntityUid uid, WoodBurnerComponent component, AtmosDeviceDisabledEvent args)
        {

        }

        /*
        private void OnToggleMessage(EntityUid uid, WoodBurnerComponent component, GasThermomachineToggleMessage args)
        {
            component.Enabled = !component.Enabled;

            DirtyUI(uid, component);
        }
        */

        private void OnRefreshParts(EntityUid uid, WoodBurnerComponent component, RefreshPartsEvent args)
        {
            var laserRating = args.PartRatings[component.MachinePartProcessingSpeed];
            var materialUseRating = args.PartRatings[component.MachinePartMaterialUse];

            //sloping down from 1/2 multiplier
            component.ProcessingSpeedMultiplier =
                component.BaseProcessingSpeedMultiplier * MathF.Pow(0.65f, laserRating - 1) + 0.1f;

            //linear increase by .1 per rating
            component.MaterialUseMultiplier = MathF.Pow(component.PartRatingMaterialUseMultiplier, materialUseRating - 1);

            component.ProcessingTimerMax = 10 + (component.BaseProcessingTimerMax * component.ProcessingSpeedMultiplier);
        }
    }
}
