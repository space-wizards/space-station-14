using System;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Interaction;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.EntitySystems
{
    public sealed class ThrusterSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

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
        }

        private void OnActivateThruster(EntityUid uid, ThrusterComponent component, ActivateInWorldEvent args)
        {
            component.EnabledVV ^= true;
        }

        private void OnRotate(EntityUid uid, ThrusterComponent component, ref RotateEvent args)
        {
            if (!component.Enabled ||
                component.Type != ThrusterType.Linear ||
                !EntityManager.TryGetComponent(uid, out TransformComponent? xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid) ||
                !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) return;

            var oldDirection = (int) args.OldRotation.Opposite().GetCardinalDir() / 2;
            var direction = (int) args.NewRotation.Opposite().GetCardinalDir() / 2;

            shuttleComponent.LinearThrusters[oldDirection] -= component.Impulse;
            shuttleComponent.LinearThrusters[direction] += component.Impulse;
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
                    var direction = (int) xform.LocalRotation.Opposite().GetCardinalDir() / 2;

                    shuttleComponent.LinearThrusters[direction] += component.Impulse;
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
        }

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
                    var direction = ((int) xform.LocalRotation.Opposite().GetCardinalDir() / 2);

                    shuttleComponent.LinearThrusters[direction] -= component.Impulse;
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
        }

        public bool CanEnable(EntityUid uid, ThrusterComponent component)
        {
            return component.EnabledVV &&
                   EntityManager.GetComponent<TransformComponent>(uid).Anchored &&
                   (!EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent? receiver) || receiver.Powered);
        }
    }
}
