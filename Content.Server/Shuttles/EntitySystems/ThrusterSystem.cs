using System;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.EntitySystems
{
    public sealed class ThrusterSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AmbientSoundSystem _ambient = default!;

        // Essentially whenever thruster enables we update the shuttle's available impulses which are used for movement.
        // This is done for each direction available.

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrusterComponent, ActivateInWorldEvent>(OnActivateThruster);
            SubscribeLocalEvent<ThrusterComponent, ComponentInit>(OnThrusterInit);
            SubscribeLocalEvent<ThrusterComponent, ComponentShutdown>(OnThrusterShutdown);
            SubscribeLocalEvent<ThrusterComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<ThrusterComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<ThrusterComponent, RotateEvent>(OnRotate);

            _mapManager.TileChanged += OnTileChange;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.TileChanged -= OnTileChange;
        }

        private void OnTileChange(object? sender, TileChangedEventArgs e)
        {
            // If the old tile was space but the new one isn't then disable all adjacent thrusters
            if (e.NewTile.IsSpace() || !e.OldTile.IsSpace()) return;

            var tilePos = e.NewTile.GridIndices;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0) continue;

                    var checkPos = tilePos + new Vector2i(x, y);

                    foreach (var ent in _mapManager.GetGrid(e.NewTile.GridIndex).GetAnchoredEntities(checkPos))
                    {
                        if (!EntityManager.TryGetComponent(ent, out ThrusterComponent? thruster)) continue;

                        // Work out if the thruster is facing this direction
                        var direction = EntityManager.GetComponent<TransformComponent>(ent).LocalRotation.ToWorldVec();

                        if (new Vector2i((int) direction.X, (int) direction.Y) != new Vector2i(x, y)) continue;

                        DisableThruster(ent, thruster);
                    }
                }
            }
        }

        private void OnActivateThruster(EntityUid uid, ThrusterComponent component, ActivateInWorldEvent args)
        {
            component.EnabledVV ^= true;
        }

        /// <summary>
        /// If the thruster rotates change the direction where the linear thrust is applied
        /// </summary>
        private void OnRotate(EntityUid uid, ThrusterComponent component, ref RotateEvent args)
        {
            // TODO: Disable visualizer for old direction

            if (!component.Enabled ||
                component.Type != ThrusterType.Linear ||
                !EntityManager.TryGetComponent(uid, out TransformComponent? xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid) ||
                !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            var oldDirection = (int) args.OldRotation.Opposite().GetCardinalDir() / 2;
            var direction = (int) args.NewRotation.Opposite().GetCardinalDir() / 2;

            shuttleComponent.LinearThrusterImpulse[oldDirection] -= component.Impulse;
            DebugTools.Assert(shuttleComponent.LinearThrusters[oldDirection].Contains(component));
            shuttleComponent.LinearThrusters[oldDirection].Remove(component);

            shuttleComponent.LinearThrusterImpulse[direction] += component.Impulse;
            DebugTools.Assert(!shuttleComponent.LinearThrusters[direction].Contains(component));
            shuttleComponent.LinearThrusters[direction].Add(component);
        }

        private void OnAnchorChange(EntityUid uid, ThrusterComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored && CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
            else
            {
                DisableThruster(uid, component);
            }
        }

        private void OnThrusterInit(EntityUid uid, ThrusterComponent component, ComponentInit args)
        {
            _ambient.SetAmbience(uid, false);

            if (!component.EnabledVV)
            {
                return;
            }

            if (CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
        }

        private void OnThrusterShutdown(EntityUid uid, ThrusterComponent component, ComponentShutdown args)
        {
            DisableThruster(uid, component);
        }

        private void OnPowerChange(EntityUid uid, ThrusterComponent component, PowerChangedEvent args)
        {
            if (args.Powered && CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
            else
            {
                DisableThruster(uid, component);
            }
        }

        /// <summary>
        /// Tries to enable the thruster and turn it on. If it's already enabled it does nothing.
        /// </summary>
        public void EnableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null)
        {
            if (component.Enabled || !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid)) return;

            component.Enabled = true;

            if (!EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            Logger.DebugS("thruster", $"Enabled thruster {uid}");

            switch (component.Type)
            {
                case ThrusterType.Linear:
                    var direction = (int) xform.LocalRotation.GetCardinalDir() / 2;

                    shuttleComponent.LinearThrusterImpulse[direction] += component.Impulse;
                    DebugTools.Assert(!shuttleComponent.LinearThrusters[direction].Contains(component));
                    shuttleComponent.LinearThrusters[direction].Add(component);
                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust += component.Impulse;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (EntityManager.TryGetComponent(uid, out SharedAppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(ThrusterVisualState.State, true);
            }

            _ambient.SetAmbience(uid, true);
        }

        /// <summary>
        /// Tries to disable the thruster.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="xform"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void DisableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null)
        {
            if (!component.Enabled ||
                !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid)) return;

            component.Enabled = false;

            if (!EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            Logger.DebugS("thruster", $"Disabled thruster {uid}");

            switch (component.Type)
            {
                case ThrusterType.Linear:
                    var direction = ((int) xform.LocalRotation.GetCardinalDir() / 2);

                    shuttleComponent.LinearThrusterImpulse[direction] -= component.Impulse;
                    DebugTools.Assert(shuttleComponent.LinearThrusters[direction].Contains(component));
                    shuttleComponent.LinearThrusters[direction].Remove(component);
                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust -= component.Impulse;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (EntityManager.TryGetComponent(uid, out SharedAppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(ThrusterVisualState.State, false);
            }

            _ambient.SetAmbience(uid, false);
        }

        public bool CanEnable(EntityUid uid, ThrusterComponent component)
        {
            if (!component.EnabledVV) return false;

            var xform = EntityManager.GetComponent<TransformComponent>(uid);

            if (!xform.Anchored ||
                (EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent? receiver) && !receiver.Powered))
            {
                return false;
            }

            var (x, y) = xform.LocalPosition + xform.LocalRotation.Opposite().ToWorldVec();
            var tile = _mapManager.GetGrid(xform.GridID).GetTileRef(new Vector2i((int) Math.Floor(x), (int) Math.Floor(y)));

            return tile.Tile.IsSpace();
        }
    }
}
