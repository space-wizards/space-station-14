using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Binary;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Binary
{
    [UsedImplicitly]
    public class GasCanisterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasCanisterComponent, ComponentStartup>(OnCanisterStartup);
            SubscribeLocalEvent<GasCanisterComponent, AtmosDeviceUpdateEvent>(OnCanisterUpdated);
            SubscribeLocalEvent<GasCanisterComponent, InteractUsingEvent>(OnCanisterInteractUsing);
            SubscribeLocalEvent<GasCanisterComponent, EntInsertedIntoContainerMessage>(OnCanisterContainerInserted);
            SubscribeLocalEvent<GasCanisterComponent, EntRemovedFromContainerMessage>(OnCanisterContainerRemoved);
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
    }
}
