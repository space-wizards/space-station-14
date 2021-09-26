using System;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Hands.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
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
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasCanisterSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

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
            // Bound UI subscriptions
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterHoldingTankEjectMessage>(OnHoldingTankEjectMessage);
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleasePressureMessage>(OnCanisterChangeReleasePressure);
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleaseValveMessage>(OnCanisterChangeReleaseValve);
        }

        private void OnCanisterStartup(EntityUid uid, GasCanisterComponent canister, ComponentStartup args)
        {
            // Ensure container manager.
            if (!ComponentManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
            {
                containerManager = ComponentManager.AddComponent<ContainerManagerComponent>(EntityManager.GetEntity(uid));
            }

            // Ensure container.
            if (!containerManager.TryGetContainer(canister.ContainerName, out _))
            {
                containerManager.MakeContainer<ContainerSlot>(canister.ContainerName);
            }
        }

        private bool CheckInteract(ICommonSession session)
        {
            if (session.AttachedEntity is not {} entity
                || !Get<ActionBlockerSystem>().CanInteract(entity)
                || !Get<ActionBlockerSystem>().CanUse(entity))
                return false;

            return true;
        }

        private void DirtyUI(EntityUid uid,
            GasCanisterComponent? canister = null, NodeContainerComponent? nodeContainer = null,
            ContainerManagerComponent? containerManager = null, IMetaDataComponent? metadata = null)
        {
            if (!Resolve(uid, ref metadata, ref canister, ref nodeContainer, ref containerManager))
                return;

            var portStatus = false;
            string? tankLabel = null;
            var tankPressure = 0f;

            if (nodeContainer.TryGetNode(canister.PortName, out PipeNode? portNode) && portNode.NodeGroup?.Nodes.Count > 1)
                portStatus = true;

            if (containerManager.TryGetContainer(canister.ContainerName, out var tankContainer)
                && tankContainer.ContainedEntities.Count > 0)
            {
                var tank = tankContainer.ContainedEntities[0];
                var tankComponent = tank.GetComponent<GasTankComponent>();
                tankLabel = tank.Name;
                tankPressure = tankComponent.Air.Pressure;
            }

            _userInterfaceSystem.TrySetUiState(uid, GasCanisterUiKey.Key,
                new GasCanisterBoundUserInterfaceState(metadata.EntityName,
                    canister.Air.Pressure, portStatus, tankLabel, tankPressure, canister.ReleasePressure,
                    canister.ReleaseValve, canister.MinReleasePressure, canister.MaxReleasePressure));
        }

        private void OnHoldingTankEjectMessage(EntityUid uid, GasCanisterComponent canister, GasCanisterHoldingTankEjectMessage args)
        {
            if (!CheckInteract(args.Session))
                return;

            if (!ComponentManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager)
                || !containerManager.TryGetContainer(canister.ContainerName, out var container))
                return;

            if (container.ContainedEntities.Count == 0)
                return;

            container.Remove(container.ContainedEntities[0]);
        }

        private void OnCanisterChangeReleasePressure(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleasePressureMessage args)
        {
            if (!CheckInteract(args.Session))
                return;

            var pressure = Math.Clamp(args.Pressure, canister.MinReleasePressure, canister.MaxReleasePressure);

            canister.ReleasePressure = pressure;
            DirtyUI(uid, canister);
        }

        private void OnCanisterChangeReleaseValve(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleaseValveMessage args)
        {
            if (!CheckInteract(args.Session))
                return;

            canister.ReleaseValve = args.Valve;
            DirtyUI(uid, canister);
        }

        private void OnCanisterUpdated(EntityUid uid, GasCanisterComponent canister, AtmosDeviceUpdateEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PortablePipeNode? portNode))
                return;

            _atmosphereSystem.React(canister.Air, portNode);

            if (portNode.NodeGroup is PipeNet {NodeCount: > 1} net)
            {
                var buffer = new GasMixture(net.Air.Volume + canister.Air.Volume);

                _atmosphereSystem.Merge(buffer, net.Air);
                _atmosphereSystem.Merge(buffer, canister.Air);

                net.Air.Clear();
                _atmosphereSystem.Merge(net.Air, buffer);
                net.Air.Multiply(net.Air.Volume / buffer.Volume);

                canister.Air.Clear();
                _atmosphereSystem.Merge(canister.Air, buffer);
                canister.Air.Multiply(canister.Air.Volume / buffer.Volume);
            }

            ContainerManagerComponent? containerManager = null;

            // Release valve is open, release gas.
            if (canister.ReleaseValve)
            {
                if (!ComponentManager.TryGetComponent(uid, out containerManager)
                    || !containerManager.TryGetContainer(canister.ContainerName, out var container))
                    return;

                if (container.ContainedEntities.Count > 0)
                {
                    var gasTank = container.ContainedEntities[0].GetComponent<GasTankComponent>();
                    _atmosphereSystem.ReleaseGasTo(canister.Air, gasTank.Air, canister.ReleasePressure);
                }
                else
                {
                    var environment = _atmosphereSystem.GetTileMixture(canister.Owner.Transform.Coordinates, true);
                    _atmosphereSystem.ReleaseGasTo(canister.Air, environment, canister.ReleasePressure);
                }
            }

            DirtyUI(uid, canister, nodeContainer, containerManager);

            // If last pressure is very close to the current pressure, do nothing.
            if (MathHelper.CloseTo(canister.Air.Pressure, canister.LastPressure))
                return;

            canister.LastPressure = canister.Air.Pressure;

            if (canister.Air.Pressure < 10)
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 0);
            }
            else if (canister.Air.Pressure < Atmospherics.OneAtmosphere)
            {
                appearance.SetData(GasCanisterVisuals.PressureState, 1);
            }
            else if (canister.Air.Pressure < (15 * Atmospherics.OneAtmosphere))
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
            if (!args.Used.TryGetComponent(out GasTankComponent? _))
                return;

            // Check the user has hands.
            if (!args.User.TryGetComponent(out HandsComponent? hands))
                return;

            if (!args.User.InRangeUnobstructed(canister, SharedInteractionSystem.InteractionRange, popup: true))
                return;

            if (!hands.TryPutEntityIntoContainer(args.Used, container))
                return;

            args.Handled = true;
        }

        private void OnCanisterContainerInserted(EntityUid uid, GasCanisterComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid, component);

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, true);
        }

        private void OnCanisterContainerRemoved(EntityUid uid, GasCanisterComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid, component);

            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(GasCanisterVisuals.TankInserted, false);
        }
    }
}
