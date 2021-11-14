using System;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Shuttles.EntitySystems
{
    public sealed class ThrusterSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrusterComponent, ComponentInit>(OnThrusterInit);
            SubscribeLocalEvent<ThrusterComponent, ComponentShutdown>(OnThrusterShutdown);
            SubscribeLocalEvent<ThrusterComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<ThrusterComponent, AnchorStateChangedEvent>(OnAnchorChange);
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
            if (!component.Enabled) return;

            component.Enabled = false;

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

        private void EnableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null)
        {
            if (component.Enabled || !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid)) return;

            component.Enabled = true;

            if (!EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            // TODO: Need to listen to rotation events and update it

            // TODO: Need to add these to a cached thing on the shuttle and also be directional
            switch (component.Type)
            {
                case ThrusterType.Linear:
                    var direction = ((int) xform.LocalRotation.GetCardinalDir() / 2);

                    shuttleComponent.LinearThrusters[direction] += component.Impulse;
                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust += component.Impulse;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // TODO: Visualizer
        }

        private void DisableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null)
        {
            if (!component.Enabled ||
                !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid)) return;

            component.Enabled = false;

            if (!EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            switch (component.Type)
            {
                case ThrusterType.Linear:
                    var direction = ((int) xform.LocalRotation.GetCardinalDir() / 2);

                    shuttleComponent.LinearThrusters[direction] -= component.Impulse;
                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust -= component.Impulse;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // TODO: Visualizer
        }

        private bool CanEnable(EntityUid uid, ThrusterComponent component)
        {
            return component.Enabled &&
                   EntityManager.GetComponent<TransformComponent>(uid).Anchored &&
                   (!EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent? receiver) || receiver.Powered);
        }
    }
}
