using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Lock;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasCanisterSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSys = default!;

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
            SubscribeLocalEvent<GasCanisterComponent, PriceCalculationEvent>(CalculateCanisterPrice);
            SubscribeLocalEvent<GasCanisterComponent, GasAnalyzerScanEvent>(OnAnalyzed);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterHoldingTankEjectMessage>(OnHoldingTankEjectMessage);
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleasePressureMessage>(OnCanisterChangeReleasePressure);
            SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleaseValveMessage>(OnCanisterChangeReleaseValve);
            SubscribeLocalEvent<GasCanisterComponent, LockToggledEvent>(OnLockToggled);

        }

        /// <summary>
        /// Completely dumps the content of the canister into the world.
        /// </summary>
        public void PurgeContents(EntityUid uid, GasCanisterComponent? canister = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref canister, ref transform))
                return;

            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);

            if (environment is not null)
                _atmosphereSystem.Merge(environment, canister.Air);

            _adminLogger.Add(LogType.CanisterPurged, LogImpact.Medium, $"Canister {ToPrettyString(uid):canister} purged its contents of {canister.Air:gas} into the environment.");
            canister.Air.Clear();
        }

        private void OnCanisterStartup(EntityUid uid, GasCanisterComponent canister, ComponentStartup args)
        {
            // Ensure container manager.
            var containerManager = EntityManager.EnsureComponent<ContainerManagerComponent>(uid);

            // Ensure container.
            if (!containerManager.TryGetContainer(canister.ContainerName, out _))
            {
                containerManager.MakeContainer<ContainerSlot>(canister.ContainerName);
            }

            if (TryComp<LockComponent>(uid, out var lockComponent))
                _appearanceSystem.SetData(uid, GasCanisterVisuals.Locked, lockComponent.Locked);
        }

        private void DirtyUI(EntityUid uid,
            GasCanisterComponent? canister = null, NodeContainerComponent? nodeContainer = null,
            ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref canister, ref nodeContainer, ref containerManager))
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
                var tankComponent = EntityManager.GetComponent<GasTankComponent>(tank);
                tankLabel = EntityManager.GetComponent<MetaDataComponent>(tank).EntityName;
                tankPressure = tankComponent.Air.Pressure;
            }

            _userInterfaceSystem.TrySetUiState(uid, GasCanisterUiKey.Key,
                new GasCanisterBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(canister.Owner).EntityName,
                    canister.Air.Pressure, portStatus, tankLabel, tankPressure, canister.ReleasePressure,
                    canister.ReleaseValve, canister.MinReleasePressure, canister.MaxReleasePressure));
        }

        private void OnHoldingTankEjectMessage(EntityUid uid, GasCanisterComponent canister, GasCanisterHoldingTankEjectMessage args)
        {
            if (!EntityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager)
                || !containerManager.TryGetContainer(canister.ContainerName, out var container))
                return;

            if (container.ContainedEntities.Count == 0)
                return;

            _adminLogger.Add(LogType.CanisterTankEjected, LogImpact.Medium, $"Player {ToPrettyString(args.Session.AttachedEntity.GetValueOrDefault()):player} ejected tank {ToPrettyString(container.ContainedEntities[0]):tank} from {ToPrettyString(uid):canister}");
            container.Remove(container.ContainedEntities[0]);
        }

        private void OnCanisterChangeReleasePressure(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleasePressureMessage args)
        {
            var pressure = Math.Clamp(args.Pressure, canister.MinReleasePressure, canister.MaxReleasePressure);

            _adminLogger.Add(LogType.CanisterPressure, LogImpact.Medium, $"{ToPrettyString(args.Session.AttachedEntity.GetValueOrDefault()):player} set the release pressure on {ToPrettyString(uid):canister} to {args.Pressure}");

            canister.ReleasePressure = pressure;
            DirtyUI(uid, canister);
        }

        private void OnCanisterChangeReleaseValve(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleaseValveMessage args)
        {
            var impact = LogImpact.High;
            if (EntityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager)
                && containerManager.TryGetContainer(canister.ContainerName, out var container))
                impact = container.ContainedEntities.Count != 0 ? LogImpact.Medium : LogImpact.High;

            var containedGasDict = new Dictionary<Gas, float>();
            var containedGasArray = Gas.GetValues(typeof(Gas));

            for (int i = 0; i < containedGasArray.Length; i++)
            {
                containedGasDict.Add((Gas)i, canister.Air.Moles[i]);
            }

            _adminLogger.Add(LogType.CanisterValve, impact, $"{ToPrettyString(args.Session.AttachedEntity.GetValueOrDefault()):player} set the valve on {ToPrettyString(uid):canister} to {args.Valve:valveState} while it contained [{string.Join(", ", containedGasDict)}]");

            canister.ReleaseValve = args.Valve;
            DirtyUI(uid, canister);
        }

        private void OnCanisterUpdated(EntityUid uid, GasCanisterComponent canister, AtmosDeviceUpdateEvent args)
        {
            _atmosphereSystem.React(canister.Air, canister);

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            if (!nodeContainer.TryGetNode(canister.PortName, out PortablePipeNode? portNode))
                return;

            if (portNode.NodeGroup is PipeNet {NodeCount: > 1} net)
            {
                MixContainerWithPipeNet(canister.Air, net.Air);
            }

            ContainerManagerComponent? containerManager = null;

            // Release valve is open, release gas.
            if (canister.ReleaseValve)
            {
                if (!EntityManager.TryGetComponent(uid, out containerManager)
                    || !containerManager.TryGetContainer(canister.ContainerName, out var container))
                    return;

                if (container.ContainedEntities.Count > 0)
                {
                    var gasTank = EntityManager.GetComponent<GasTankComponent>(container.ContainedEntities[0]);
                    _atmosphereSystem.ReleaseGasTo(canister.Air, gasTank.Air, canister.ReleasePressure);
                }
                else
                {
                    var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
                    _atmosphereSystem.ReleaseGasTo(canister.Air, environment, canister.ReleasePressure);
                }
            }

            // If last pressure is very close to the current pressure, do nothing.
            if (MathHelper.CloseToPercent(canister.Air.Pressure, canister.LastPressure))
                return;

            DirtyUI(uid, canister, nodeContainer, containerManager);

            canister.LastPressure = canister.Air.Pressure;

            if (canister.Air.Pressure < 10)
            {
                _appearanceSystem.SetData(uid, GasCanisterVisuals.PressureState, 0, appearance);
            }
            else if (canister.Air.Pressure < Atmospherics.OneAtmosphere)
            {
                _appearanceSystem.SetData(uid, GasCanisterVisuals.PressureState, 1, appearance);
            }
            else if (canister.Air.Pressure < (15 * Atmospherics.OneAtmosphere))
            {
                _appearanceSystem.SetData(uid, GasCanisterVisuals.PressureState, 2, appearance);
            }
            else
            {
                _appearanceSystem.SetData(uid, GasCanisterVisuals.PressureState, 3, appearance);
            }
        }

        private void OnCanisterActivate(EntityUid uid, GasCanisterComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;
            if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            {
                _popupSystem.PopupEntity(Loc.GetString("gas-canister-popup-denied"), uid, args.User);
                if (component.AccessDeniedSound != null)
                    _audioSys.PlayPvs(component.AccessDeniedSound, uid);
                return;
            }

            _userInterfaceSystem.TryOpen(uid, GasCanisterUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }


        private void OnCanisterInteractHand(EntityUid uid, GasCanisterComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;
            if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
                return;

            _userInterfaceSystem.TryOpen(uid, GasCanisterUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }

        private void OnCanisterInteractUsing(EntityUid canister, GasCanisterComponent component, InteractUsingEvent args)
        {
            var container = _containerSystem.EnsureContainer<ContainerSlot>(canister, component.ContainerName);

            // Container full.
            if (container.ContainedEntity != null)
                return;

            // Check the used item is valid...
            if (!EntityManager.TryGetComponent(args.Used, out GasTankComponent? _))
                return;

            if (!_handsSystem.TryDropIntoContainer(args.User, args.Used, container))
                return;

            _adminLogger.Add(LogType.CanisterTankInserted, LogImpact.Medium, $"Player {ToPrettyString(args.User):player} inserted tank {ToPrettyString(container.ContainedEntities[0]):tank} into {ToPrettyString(canister):canister}");

            args.Handled = true;
        }

        private void OnCanisterContainerInserted(EntityUid uid, GasCanisterComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid, component);

            _appearanceSystem.SetData(uid, GasCanisterVisuals.TankInserted, true);
        }

        private void OnCanisterContainerRemoved(EntityUid uid, GasCanisterComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ContainerName)
                return;

            DirtyUI(uid, component);

            _appearanceSystem.SetData(uid, GasCanisterVisuals.TankInserted, false);
        }

        /// <summary>
        /// Mix air from a gas container into a pipe net.
        /// Useful for anything that uses connector ports.
        /// </summary>
        public void MixContainerWithPipeNet(GasMixture containerAir, GasMixture pipeNetAir)
        {
            var buffer = new GasMixture(pipeNetAir.Volume + containerAir.Volume);

            _atmosphereSystem.Merge(buffer, pipeNetAir);
            _atmosphereSystem.Merge(buffer, containerAir);

            pipeNetAir.Clear();
            _atmosphereSystem.Merge(pipeNetAir, buffer);
            pipeNetAir.Multiply(pipeNetAir.Volume / buffer.Volume);

            containerAir.Clear();
            _atmosphereSystem.Merge(containerAir, buffer);
            containerAir.Multiply(containerAir.Volume / buffer.Volume);
        }

        private void CalculateCanisterPrice(EntityUid uid, GasCanisterComponent component, ref PriceCalculationEvent args)
        {
            args.Price += _atmosphereSystem.GetPrice(component.Air);
        }

                /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnAnalyzed(EntityUid uid, GasCanisterComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures = new Dictionary<string, GasMixture?> { {Name(uid), component.Air} };
        }

        private void OnLockToggled(EntityUid uid, GasCanisterComponent component, LockToggledEvent args)
        {
            _appearanceSystem.SetData(uid, GasCanisterVisuals.Locked, args.Locked);
        }
    }
}
