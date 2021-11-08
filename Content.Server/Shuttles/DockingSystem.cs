using System;
using System.Collections.Generic;
using Content.Server.Doors.Components;
using Content.Server.Power.Components;
using Content.Shared.Doors;
using Content.Shared.Physics;
using Content.Shared.Shuttles;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles
{
    [RegisterComponent]
    public sealed class DockingComponent : SharedDockingComponent
    {
        [ViewVariables]
        public DockingComponent? DockedWith;

        [ViewVariables]
        public WeldJoint? DockJoint;

        [ViewVariables]
        public override bool Docked => DockedWith != null;
    }

    public sealed class DockingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.3f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);

            SubscribeLocalEvent<DockingComponent, GetInteractionVerbsEvent>(OnVerb);
            SubscribeLocalEvent<DockingComponent, BeforeDoorAutoCloseEvent>(OnAutoClose);
            SubscribeLocalEvent<DockingComponent, DoorOpenAttemptEvent>(OnDoorOpenAttempt);
            SubscribeLocalEvent<DockingComponent, DoorCloseAttemptEvent>(OnDoorCloseAttempt);
        }

        // Won't allow users to override door controls
        private void OnDoorOpenAttempt(EntityUid uid, DockingComponent component, DoorOpenAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDoorCloseAttempt(EntityUid uid, DockingComponent component, DoorCloseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnAutoClose(EntityUid uid, DockingComponent component, BeforeDoorAutoCloseEvent args)
        {
            // We'll just pin the door open when docked.
            if (component.Docked)
                args.Cancel();
        }

        private void OnVerb(EntityUid uid, DockingComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanInteract ||
                !args.CanAccess) return;

            Verb verb = new();

            // TODO: Have it open the UI and have the UI do this.
            if (component.Enabled &&
                !component.Docked &&
                EntityManager.TryGetComponent(uid, out PhysicsComponent? body) &&
                EntityManager.TryGetComponent(uid, out ITransformComponent? xform))
            {
                var shuttles = GetDockable(body, xform);

                verb = new Verb
                {
                    Disabled = shuttles == null,
                    Text = "Dock", // TODO Loc I know
                    Act = () =>
                    {
                        if (shuttles == null) return;
                        TryDock(shuttles.Value.Item1, shuttles.Value.Item2);
                    }
                };
            }
            else if (component.Docked)
            {
                verb = new Verb
                {
                    Disabled = !component.Docked,
                    Text = "Undock",
                    Act = () =>
                    {
                        Undock(component);
                    }
                };
            }

            args.Verbs.Add(verb);
        }

        private (ShuttleComponent, ShuttleComponent)? GetDockable(PhysicsComponent body, ITransformComponent dockingXform)
        {
            if (!_mapManager.TryGetGrid(dockingXform.GridID, out var grid) ||
                !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleA)) return null;

            var transform = body.GetTransform();
            var dockingFixture = body.GetFixture(DockingFixture)!;

            foreach (var (otherDocking, otherBody, otherXform) in EntityManager.EntityQuery<DockingComponent, PhysicsComponent, ITransformComponent>())
            {
                if (!otherDocking.Enabled ||
                    otherDocking.Docked ||
                    dockingXform.MapID != otherXform.MapID ||
                    dockingXform.GridID == otherXform.GridID ||
                    !_mapManager.TryGetGrid(otherXform.GridID, out var otherGrid) ||
                    !EntityManager.TryGetComponent(otherGrid.GridEntityId, out ShuttleComponent? shuttleB)) continue;

                var otherTransform = otherBody.GetTransform();
                var otherDockingFixture = otherBody.GetFixture(DockingFixture);

                for (var i = 0; i < dockingFixture.Shape.ChildCount; i++)
                {
                    var aabb = dockingFixture.Shape.ComputeAABB(transform, i);

                    for (var j = 0; j < dockingFixture.Shape.ChildCount; j++)
                    {
                        var otherAABB = otherDockingFixture!.Shape.ComputeAABB(otherTransform, j);

                        if (!aabb.Intersects(otherAABB)) continue;

                        return (shuttleA, shuttleB);
                    }
                }
            }

            return null;
        }

        private void OnShutdown(EntityUid uid, DockingComponent component, ComponentShutdown args)
        {
            if (component.DockJoint == null ||
                component.DockedWith == null ||
                EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage > EntityLifeStage.MapInitialized) return;

            Cleanup(component);
        }

        private void Cleanup(DockingComponent component)
        {
            _jointSystem.RemoveJoint(component.DockJoint!);

            component.DockedWith!.DockedWith = null;
            component.DockedWith.DockJoint = null;

            component.DockJoint = null;
            component.DockedWith = null;
        }

        private void OnStartup(EntityUid uid, DockingComponent component, ComponentStartup args)
        {
            // Use startup so transform already initialized
            EnableDocking(uid, component);
        }

        private void OnAnchorChange(EntityUid uid, DockingComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                EnableDocking(uid, component);
            }
            else
            {
                DisableDocking(uid, component);
            }
        }

        private void OnPowerChange(EntityUid uid, DockingComponent component, PowerChangedEvent args)
        {
            if (args.Powered)
            {
                EnableDocking(uid, component);
            }
            else
            {
                DisableDocking(uid, component);
            }
        }

        private void DisableDocking(EntityUid uid, DockingComponent component)
        {
            if (!component.Enabled) return;

            component.Enabled = false;

            if (component.DockedWith != null)
            {
                Undock(component);
            }

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            // TODO: Break
            _broadphaseSystem.DestroyFixture(physicsComponent, DockingFixture);
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled) return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            component.Enabled = true;

            var xform = EntityManager.GetComponent<ITransformComponent>(uid);

            var shape = new PhysShapeCircle
            {
                // Want half of the unit vector
                Position = xform.LocalRotation.ToWorldVec() / 2f,
                Radius = DockingRadius
            };

            // TODO: Its own collision mask / layer probably
            var fixture = new Fixture(physicsComponent, shape)
            {
                ID = DockingFixture,
                Hard = false,
                CollisionMask = (int) CollisionGroup.Docking,
                CollisionLayer = (int) CollisionGroup.Docking,
            };

            _broadphaseSystem.CreateFixture(physicsComponent, fixture);
        }

        private void Dock(DockingComponent dockA, DockingComponent dockB)
        {
            var gridA = _mapManager.GetGrid(dockA.Owner.Transform.GridID).GridEntityId;
            var gridB = _mapManager.GetGrid(dockB.Owner.Transform.GridID).GridEntityId;

            var dockAXform = EntityManager.GetComponent<ITransformComponent>(dockA.OwnerUid);
            var dockBXform = EntityManager.GetComponent<ITransformComponent>(dockB.OwnerUid);

            Logger.DebugS("docking", $"Docking between {dockA.Owner} and {dockB.Owner}");

            var weld = _jointSystem.CreateWeldJoint(gridA, gridB, DockingJoint);

            weld.LocalAnchorA = dockBXform.LocalPosition + dockBXform.LocalRotation.ToWorldVec() / 2f;
            weld.LocalAnchorB = dockAXform.LocalPosition + dockAXform.LocalRotation.ToWorldVec() / 2f;
            // TODO: Get the cardinal you numpty
            weld.ReferenceAngle = (float) dockAXform.LocalRotation.Theta - MathF.PI / 2;
            weld.CollideConnected = false;

            dockA.DockedWith = dockB;
            dockB.DockedWith = dockA;
            dockA.DockJoint = weld;
            dockB.DockJoint = weld;
        }

        public bool TryDock(ShuttleComponent shuttleA, ShuttleComponent shuttleB)
        {
            var shuttleAXform = EntityManager.GetComponent<ITransformComponent>(shuttleA.OwnerUid);
            var shuttleBXform = EntityManager.GetComponent<ITransformComponent>(shuttleB.OwnerUid);
            var dockingPorts = new List<(DockingComponent, PhysicsComponent, ITransformComponent)>();

            // Get the relevant ports for shuttle A and shuttle B
            foreach (var (docking, body, xform) in EntityManager.EntityQuery<DockingComponent, PhysicsComponent, ITransformComponent>())
            {
                if (xform.GridID != shuttleAXform.GridID &&
                    xform.GridID != shuttleBXform.GridID ||
                    !docking.Enabled ||
                    docking.DockedWith != null ||
                    body.GetFixture(DockingFixture) == null) continue;

                dockingPorts.Add((docking, body, xform));
            }

            var intersectingPorts = new List<(DockingComponent, DockingComponent)>();

            // Check for any docking collisions
            foreach (var (docking, body, xform) in dockingPorts)
            {
                if (xform.GridID != shuttleAXform.GridID) continue;

                var transform = body.GetTransform();
                var dockingFixture = body.GetFixture(DockingFixture)!;

                foreach (var (otherDocking, otherBody, otherXform) in dockingPorts)
                {
                    if (otherXform.GridID == xform.GridID ||
                        otherDocking.DockedWith != null) continue;

                    var otherTransform = otherBody.GetTransform();
                    var otherDockingFixture = otherBody.GetFixture(DockingFixture);

                    for (var i = 0; i < dockingFixture.Shape.ChildCount; i++)
                    {
                        var aabb = dockingFixture.Shape.ComputeAABB(transform, i);

                        for (var j = 0; j < dockingFixture.Shape.ChildCount; j++)
                        {
                            var otherAABB = otherDockingFixture!.Shape.ComputeAABB(otherTransform, j);

                            if (!aabb.Intersects(otherAABB)) continue;

                            intersectingPorts.Add((docking, otherDocking));
                            break;
                        }
                    }
                }
            }

            if (intersectingPorts.Count == 0) return false;

            // Docking confirmed

            foreach (var (dockA, dockB) in intersectingPorts)
            {
                if (EntityManager.TryGetComponent(dockA.OwnerUid, out ServerDoorComponent? doorA))
                {
                    doorA.Open();
                }

                if (EntityManager.TryGetComponent(dockB.OwnerUid, out ServerDoorComponent? doorB))
                {
                    doorB.Open();
                }

                Dock(dockA, dockB);
            }

            EntityManager.EventBus.RaiseEvent(EventSource.Local, new DockEvent
            {
                ShuttleA = shuttleA,
                ShuttleB = shuttleB
            });

            return true;
        }

        private void Undock(DockingComponent dock)
        {
            if (dock.DockedWith == null)
            {
                DebugTools.Assert(false);
                Logger.ErrorS("docking", $"Tried to undock {dock.OwnerUid} but not docked with anything?");
                return;
            }

            if (EntityManager.TryGetComponent(dock.OwnerUid, out ServerDoorComponent? doorA))
            {
                doorA.Close();
            }

            if (EntityManager.TryGetComponent(dock.DockedWith.OwnerUid, out ServerDoorComponent? doorB))
            {
                doorB.Close();
            }

            Cleanup(dock);
        }

        public bool TryUndock(ShuttleComponent shuttleA, ShuttleComponent shuttleB)
        {
            var shuttleAXform = EntityManager.GetComponent<ITransformComponent>(shuttleA.OwnerUid);
            var shuttleBXform = EntityManager.GetComponent<ITransformComponent>(shuttleB.OwnerUid);

            var undocked = false;

            foreach (var (docking, body, xform) in EntityManager
                .EntityQuery<DockingComponent, PhysicsComponent, ITransformComponent>())
            {
                // We'll always just check ShuttleA's docks and assume all of ShuttleB's will be cleaned up as a result.
                if (shuttleAXform.GridID != xform.GridID ||
                    docking.DockedWith == null) continue;

                Undock(docking);
                undocked = true;
            }

            if (!undocked) return false;

            EntityManager.EventBus.RaiseEvent(EventSource.Local, new UndockEvent
            {
                ShuttleA = shuttleA,
                ShuttleB = shuttleB
            });

            return true;
        }

        /// <summary>
        /// Raised whenever 2 grids dock.
        /// </summary>
        public sealed class DockEvent : EntityEventArgs
        {
            public ShuttleComponent ShuttleA = default!;
            public ShuttleComponent ShuttleB = default!;
        }

        /// <summary>
        /// Raised whenever 2 grids undock.
        /// </summary>
        public sealed class UndockEvent : EntityEventArgs
        {
            public ShuttleComponent ShuttleA = default!;
            public ShuttleComponent ShuttleB = default!;
        }
    }
}
