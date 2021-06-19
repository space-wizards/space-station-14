using System;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Hands.Components;
using Content.Server.NodeContainer;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasCanisterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasCanisterComponent, ComponentStartup>(OnCanisterStartup);
            SubscribeLocalEvent<GasCanisterComponent, AtmosDeviceUpdateEvent>(OnCanisterUpdated);
            SubscribeLocalEvent<GasCanisterComponent, ActivateInWorldEvent>(OnCanisterActivate);
            SubscribeLocalEvent<GasCanisterComponent, InteractHandEvent>(OnCanisterInteractHand);
            SubscribeLocalEvent<GasCanisterComponent, InteractUsingEvent>(OnCanisterInteractUsing);
            SubscribeLocalEvent<GasCanisterComponent, EntInsertedIntoContainerMessage>(OnCanisterContainerInserted);
            SubscribeLocalEvent<GasCanisterComponent, EntRemovedFromContainerMessage>(OnCanisterContainerRemoved);
        }

        private void OnCanisterStartup(EntityUid uid, GasCanisterComponent canister, ComponentStartup args)
        {
            // TODO ATMOS: Don't use Owner to get the UI.
            if(canister.Owner.GetUIOrNull(GasCanisterUiKey.Key) is {} ui)
                ui.OnReceiveMessage += msg => OnCanisterUIMessage(uid, canister, msg);

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode))
                return;

            // Create a pipenet if we don't have one already.
            portNode.TryAssignGroupIfNeeded();
            portNode.Air.Merge(canister.InitialMixture);
            portNode.Air.Temperature = canister.InitialMixture.Temperature;
            portNode.Volume = canister.InitialMixture.Volume;
        }

        private void DirtyUI(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out IMetaDataComponent? metadata)
            || !ComponentManager.TryGetComponent(uid, out GasCanisterComponent? canister)
            || !ComponentManager.TryGetComponent(uid, out GasPassiveGateComponent? passiveGate)
            || !ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode)
            || !nodeContainer.TryGetNode(canister.TankName, out PipeNode? tankNode)
            || !ComponentManager.TryGetComponent(uid, out ServerUserInterfaceComponent? userInterfaceComponent)
            || !userInterfaceComponent.TryGetBoundUserInterface(GasCanisterUiKey.Key, out var ui))
                return;

            string? tankLabel = null;
            var tankPressure = 0f;

            if (ComponentManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager) && containerManager.TryGetContainer(canister.ContainerName, out var tankContainer) && tankContainer.ContainedEntities.Count > 0)
            {
                var tank = tankContainer.ContainedEntities[0];
                tankLabel = tank.Name;
                tankPressure = tankNode.Air.Pressure;
            }

            ui.SetState(new GasCanisterBoundUserInterfaceState(metadata.EntityName, portNode.Air.Pressure,
                portNode.NodeGroup.Nodes.Count > 1, tankLabel, tankPressure,
                passiveGate.TargetPressure, passiveGate.Enabled,
                canister.MinReleasePressure, canister.MaxReleasePressure));
        }

        private void OnCanisterUIMessage(EntityUid uid, GasCanisterComponent canister, ServerBoundUserInterfaceMessage msg)
        {
            if (msg.Session.AttachedEntity is not {} entity
                || !Get<ActionBlockerSystem>().CanInteract(entity)
                || !Get<ActionBlockerSystem>().CanUse(entity))
                return;


            if (!ComponentManager.TryGetComponent(uid, out GasPassiveGateComponent? passiveGate)
            || !ComponentManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager)
            || !containerManager.TryGetContainer(canister.ContainerName, out var container))
                return;

            switch (msg.Message)
            {
                case GasCanisterHoldingTankEjectMessage:
                    if (container.ContainedEntities.Count == 0)
                        break;

                    container.Remove(container.ContainedEntities[0]);
                    break;

                case GasCanisterChangeReleasePressureMessage changeReleasePressure:
                    var pressure = Math.Clamp(changeReleasePressure.Pressure, canister.MinReleasePressure, canister.MaxReleasePressure);

                    passiveGate.TargetPressure = pressure;
                    DirtyUI(uid);
                    break;

                case GasCanisterChangeReleaseValveMessage changeReleaseValve:
                    passiveGate.Enabled = changeReleaseValve.Valve;
                    DirtyUI(uid);
                    break;
            }
        }

        private void OnCanisterUpdated(EntityUid uid, GasCanisterComponent canister, AtmosDeviceUpdateEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                ||  !ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode))
                return;

            DirtyUI(uid);

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

        private void OnCanisterActivate(EntityUid uid, GasCanisterComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.Owner.GetUIOrNull(GasCanisterUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }


        private void OnCanisterInteractHand(EntityUid uid, GasCanisterComponent component, InteractHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.Owner.GetUIOrNull(GasCanisterUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnCanisterInteractUsing(EntityUid uid, GasCanisterComponent component, InteractUsingEvent args)
        {
            var canister = EntityManager.GetEntity(uid);
            var container = canister.EnsureContainer<ContainerSlot>(component.ContainerName);

            // Container full.
            if (container.ContainedEntity != null)
                return;

            // Check the used item is valid...
            if (!args.Used.TryGetComponent(out GasTankComponent? _)
                || !args.Used.TryGetComponent(out NodeContainerComponent? _))
                return;

            // Check the user has hands.
            if (!args.User.TryGetComponent(out HandsComponent? hands))
                return;

            if (!args.User.InRangeUnobstructed(canister, SharedInteractionSystem.InteractionRange, popup: true))
                return;

            if (!hands.Drop(args.Used, canister.Transform.Coordinates))
                return;

            if (!container.Insert(args.Used))
                return;

            args.Handled = true;
        }

        private void OnCanisterContainerInserted(EntityUid uid, GasCanisterComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid);

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(component.TankName, out PipeNode? tankNode))
                return;

            tankNode.EnvironmentalAir = false;
            tankNode.ConnectToContainedEntities = true;
            tankNode.NodeGroup.RemakeGroup();

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, true);
        }

        private void OnCanisterContainerRemoved(EntityUid uid, GasCanisterComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid);

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(component.TankName, out PipeNode? tankNode))
                return;

            tankNode.NodeGroup.RemakeGroup();
            tankNode.ConnectToContainedEntities = false;
            tankNode.EnvironmentalAir = true;

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, false);
        }
    }
}
