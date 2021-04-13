#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Schema;
using Content.Server.Atmos;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.Interfaces.GameObjects;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();

        private GridTileLookupSystem? _gridTileLookup = null;

        private const float ExposedUpdateDelay = 1f;
        private float _exposedTimer = 0f;

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;

        private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];
        public float[] GasSpecificHeats => _gasSpecificHeats;

        public GridTileLookupSystem GridTileLookupSystem => _gridTileLookup ??= Get<GridTileLookupSystem>();

        public override void Initialize()
        {
            base.Initialize();

            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

            for (var i = 0; i < GasPrototypes.Length; i++)
            {
                _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat;
            }

            #region CVars

            _cfg.OnValueChanged(CCVars.SpaceWind, OnSpaceWindChanged, true);
            _cfg.OnValueChanged(CCVars.MonstermosEqualization, OnMonstermosEqualizationChanged, true);
            _cfg.OnValueChanged(CCVars.Superconduction, OnSuperconductionChanged, true);
            _cfg.OnValueChanged(CCVars.AtmosMaxProcessTime, OnAtmosMaxProcessTimeChanged, true);
            _cfg.OnValueChanged(CCVars.AtmosTickRate, OnAtmosTickRateChanged, true);
            _cfg.OnValueChanged(CCVars.ExcitedGroupsSpaceIsAllConsuming, OnExcitedGroupsSpaceIsAllConsumingChanged, true);

            #endregion

            #region Events

            // Map events.
            _mapManager.MapCreated += OnMapCreated;
            _mapManager.TileChanged += OnTileChanged;

            // Airtight entities.
            SubscribeLocalEvent<AirtightComponent, SnapGridPositionChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);

            // Atmos devices.
            SubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnDeviceBodyTypeChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AtmosDeviceUpdateEvent>(OnDeviceAtmosProcess);
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);

            #endregion
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= OnMapCreated;
            _mapManager.TileChanged -= OnTileChanged;

            UnsubscribeLocalEvent<AirtightComponent, SnapGridPositionChangedEvent>(OnAirtightPositionChanged);
            UnsubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);

            UnsubscribeLocalEvent<AtmosDeviceComponent, PhysicsBodyTypeChangedEvent>(OnDeviceBodyTypeChanged);
            UnsubscribeLocalEvent<AtmosDeviceComponent, AtmosDeviceUpdateEvent>(OnDeviceAtmosProcess);
            UnsubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
        }

        #region CVars
        public bool SpaceWind { get; private set; }
        public bool MonstermosEqualization { get; private set; }
        public bool Superconduction { get; private set; }
        public bool ExcitedGroupsSpaceIsAllConsuming { get; private set; }
        public float AtmosMaxProcessTime { get; private set; }
        public float AtmosTickRate { get; private set; }

        private void OnExcitedGroupsSpaceIsAllConsumingChanged(bool obj)
        {
            ExcitedGroupsSpaceIsAllConsuming = obj;
        }

        private void OnAtmosTickRateChanged(float obj)
        {
            AtmosTickRate = obj;
        }

        private void OnAtmosMaxProcessTimeChanged(float obj)
        {
            AtmosMaxProcessTime = obj;
        }

        private void OnMonstermosEqualizationChanged(bool obj)
        {
            MonstermosEqualization = obj;
        }

        private void OnSuperconductionChanged(bool obj)
        {
            Superconduction = obj;
        }

        private void OnSpaceWindChanged(bool obj)
        {
            SpaceWind = obj;
        }
        #endregion

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.IsSpace() == eventArgs.OldTile.IsSpace())
            {
                return;
            }

            GetGridAtmosphere(eventArgs.NewTile.GridIndex)?.Invalidate(eventArgs.NewTile.GridIndices);
        }

        private void OnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
                return;

            var map = _mapManager.GetMapEntity(e.Map);

            if (!map.HasComponent<IGridAtmosphereComponent>())
                map.AddComponent<SpaceGridAtmosphereComponent>();
        }

        #region Airtight Handlers
        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent component, SnapGridPositionChangedEvent args)
        {
            component.OnSnapGridMove(args);
        }

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, RotateEvent ev)
        {
            airtight.RotateEvent(ev);
        }
        #endregion

        #region Atmos Device Handlers
        private void OnDeviceAtmosProcess(EntityUid uid, AtmosDeviceComponent component, AtmosDeviceUpdateEvent _)
        {
            if (component.Atmosphere == null)
                return; // Shouldn't really happen, but just in case...

            var time = (float)component.DeltaTime.TotalSeconds;

            foreach (var process in ComponentManager.GetComponents<IAtmosProcess>(uid))
            {
                process.ProcessAtmos(time, component.Atmosphere);
            }
        }

        private void OnDeviceBodyTypeChanged(EntityUid uid, AtmosDeviceComponent component, PhysicsBodyTypeChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!component.RequireAnchored)
                return;

            if (args.Anchored)
                component.JoinAtmosphere();
            else
                component.LeaveAtmosphere();
        }

        private void OnDeviceParentChanged(EntityUid uid, AtmosDeviceComponent component, EntParentChangedMessage args)
        {
            component.RejoinAtmosphere();
        }
        #endregion

        #region Helper Methods
        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            if (!gridId.IsValid())
                return null;

            if (!_mapManager.TryGetGrid(gridId, out var grid))
                return null;

            return ComponentManager.TryGetComponent(grid.GridEntityId, out IGridAtmosphereComponent? gridAtmosphere)
                ? gridAtmosphere : null;
        }

        public IGridAtmosphereComponent GetGridAtmosphere(EntityCoordinates coordinates)
        {
            return GetGridAtmosphere(coordinates.ToMap(EntityManager));
        }

        public IGridAtmosphereComponent GetGridAtmosphere(MapCoordinates coordinates)
        {
            if (coordinates.MapId == MapId.Nullspace)
            {
                throw new ArgumentException($"Coordinates cannot be in nullspace!", nameof(coordinates));
            }

            if (_mapManager.TryFindGridAt(coordinates, out var grid))
            {
                if (ComponentManager.TryGetComponent(grid.GridEntityId, out IGridAtmosphereComponent? atmos))
                {
                    return atmos;
                }
            }

            return _mapManager.GetMapEntity(coordinates.MapId).GetComponent<IGridAtmosphereComponent>();
        }

        /// <summary>
        ///     Unlike <see cref="GetGridAtmosphere"/>, this doesn't return space grid when not found.
        /// </summary>
        public bool TryGetSimulatedGridAtmosphere(MapCoordinates coordinates, [NotNullWhen(true)] out IGridAtmosphereComponent? atmosphere)
        {
            if (coordinates.MapId == MapId.Nullspace)
            {
                atmosphere = null;
                return false;
            }

            if (_mapManager.TryFindGridAt(coordinates, out var mapGrid)
                && ComponentManager.TryGetComponent(mapGrid.GridEntityId, out IGridAtmosphereComponent? atmosGrid)
                && atmosGrid.Simulated)
            {
                atmosphere = atmosGrid;
                return true;
            }

            if (_mapManager.GetMapEntity(coordinates.MapId).TryGetComponent(out IGridAtmosphereComponent? atmosMap)
                && atmosMap.Simulated)
            {
                atmosphere = atmosMap;
                return true;
            }

            atmosphere = null;
            return false;
        }
        #endregion

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _exposedTimer += frameTime;

            foreach (var (mapGridComponent, gridAtmosphereComponent) in EntityManager.ComponentManager.EntityQuery<IMapGridComponent, IGridAtmosphereComponent>(true))
            {
                if (_pauseManager.IsGridPaused(mapGridComponent.GridIndex)) continue;

                gridAtmosphereComponent.Update(frameTime);
            }

            if (_exposedTimer >= ExposedUpdateDelay)
            {
                foreach (var exposed in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>(true))
                {
                    var tile = exposed.Owner.Transform.Coordinates.GetTileAtmosphere();
                    if (tile == null) continue;
                    exposed.Update(tile, _exposedTimer);
                }

                _exposedTimer = 0;
            }
        }
    }
}
