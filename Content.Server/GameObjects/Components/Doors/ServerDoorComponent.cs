#nullable enable
using System;
using System.Linq;
using System.Threading;
using Content.Server.Atmos;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ServerDoorComponent : Component, IActivate, ICollideBehavior, IInteractUsing
    {
        public override string Name => "Door";

        [ViewVariables]
        private DoorState _state = DoorState.Closed;

        public virtual DoorState State
        {
            get => _state;
            protected set
            {
                if (_state == value)
                    return;

                _state = value;

                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new DoorStateMessage(this, State));
            }
        }

        [ViewVariables]
        protected float OpenTimeCounter;
        [ViewVariables(VVAccess.ReadWrite)]
        protected bool AutoClose = true;
        protected const float AutoCloseDelay = 5;
        [ViewVariables(VVAccess.ReadWrite)]
        protected float CloseSpeed = AutoCloseDelay;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected virtual TimeSpan CloseTimeOne => TimeSpan.FromSeconds(0.3f);
        protected virtual TimeSpan CloseTimeTwo => TimeSpan.FromSeconds(0.9f);
        protected virtual TimeSpan OpenTimeOne => TimeSpan.FromSeconds(0.3f);
        protected virtual TimeSpan OpenTimeTwo => TimeSpan.FromSeconds(0.9f);
        protected virtual TimeSpan DenyTime => TimeSpan.FromSeconds(0.45f);

        private const int DoorCrushDamage = 15;
        private const float DoorStunTime = 5f;
        [ViewVariables(VVAccess.ReadWrite)]
        protected bool Safety = true;

        [ViewVariables(VVAccess.ReadWrite)] private bool _occludes;

        public bool Occludes => _occludes;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                if (_isWeldedShut == value)
                {
                    return;
                }

                _isWeldedShut = value;
                SetAppearance(_isWeldedShut ? DoorVisualState.Welded : DoorVisualState.Closed);
            }
        }
        private bool _isWeldedShut;

        private bool _canWeldShut = true;

        /// <summary>
        ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
        /// </summary>
        private bool _beingWelded = false;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _canCrush = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _occludes, "occludes", true);
            serializer.DataField(ref _isWeldedShut, "welded", false);
            serializer.DataField(ref _canCrush, "canCrush", true);
        }

        public override void OnRemove()
        {
            _cancellationTokenSource?.Cancel();

            base.OnRemove();
        }

        protected virtual void ActivateImpl(ActivateEventArgs eventArgs)
        {
            if (State == DoorState.Open)
            {
                TryClose(eventArgs.User);
            }
            else if (State == DoorState.Closed)
            {
                TryOpen(eventArgs.User);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ActivateImpl(eventArgs);
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            // Disabled because it makes it suck hard to walk through double doors.

            if (entity.HasComponent<IBody>())
            {
                if (!entity.TryGetComponent<IMoverComponent>(out var mover)) return;

                /*
                // TODO: temporary hack to fix the physics system raising collision events akwardly.
                // E.g. when moving parallel to a door by going off the side of a wall.
                var (walking, sprinting) = mover.VelocityDir;
                // Also TODO: walking and sprint dir are added together here
                // instead of calculating their contribution correctly.
                var dotProduct = Vector2.Dot((sprinting + walking).Normalized, (entity.Transform.WorldPosition - Owner.Transform.WorldPosition).Normalized);
                if (dotProduct <= -0.85f)
                    TryOpen(entity);
                */

                TryOpen(entity);
            }
        }

        protected void SetAppearance(DoorVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.VisualState, state);
            }
        }

        public virtual bool CanOpen()
        {
            return !_isWeldedShut;
        }

        public virtual bool CanOpen(IEntity user)
        {
            if (!CanOpen()) return false;

            if (!Owner.TryGetComponent<AccessReader>(out var accessReader))
            {
                return true;
            }

            var doorSystem = EntitySystem.Get<DoorSystem>();
            var isAirlockExternal = HasAccessType("External");

            return doorSystem.AccessType switch
            {
                DoorSystem.AccessTypes.AllowAll => true,
                DoorSystem.AccessTypes.AllowAllIdExternal => isAirlockExternal ? accessReader.IsAllowed(user) : true,
                DoorSystem.AccessTypes.AllowAllNoExternal => !isAirlockExternal,
                _ => accessReader.IsAllowed(user)
            };
        }

        /// <summary>
        /// Returns whether a door has a certain access type. For example, maintenance doors will have access type
        /// "Maintenance" in their AccessReader.
        /// </summary>
        private bool HasAccessType(string accesType)
        {
            if(Owner.TryGetComponent<AccessReader>(out var accessReader))
            {
                return accessReader.AccessLists.Any(list => list.Contains(accesType));
            }

            return true;
        }

        public void TryOpen(IEntity user)
        {
            if (CanOpen(user))
            {
                Open();

                if (user.TryGetComponent(out HandsComponent? hands) && hands.Count == 0)
                {
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/bang.ogg", Owner,
                                                                   AudioParams.Default.WithVolume(-2));
                }
            }
            else
            {
                Deny();
            }
        }

        public void Open()
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            _canWeldShut = false;
            State = DoorState.Opening;
            SetAppearance(DoorVisualState.Opening);
            if (_occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }

            Timer.Spawn(OpenTimeOne, async () =>
            {
                if (Owner.TryGetComponent(out AirtightComponent? airtight))
                {
                    airtight.AirBlocked = false;
                }

                if (Owner.TryGetComponent(out IPhysicsComponent? physics))
                {
                    physics.CanCollide = false;
                }

                await Timer.Delay(OpenTimeTwo, _cancellationTokenSource.Token);

                State = DoorState.Open;
                SetAppearance(DoorVisualState.Open);
            }, _cancellationTokenSource.Token);

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, false));
        }

        public virtual bool CanClose()
        {
            return true;
        }

        public virtual bool CanClose(IEntity user)
        {
            if (!CanClose()) return false;
            if (!Owner.TryGetComponent(out AccessReader? accessReader))
            {
                return true;
            }

            return accessReader.IsAllowed(user);
        }

        public void TryClose(IEntity user)
        {
            if (!CanClose(user))
            {
                Deny();
                return;
            }

            Close();
        }

        private void CheckCrush()
        {
            if (!Owner.TryGetComponent(out IPhysicsComponent? body))
                return;

            // Crush
            foreach (var e in body.GetCollidingEntities(Vector2.Zero, false))
            {
                if (!e.TryGetComponent(out StunnableComponent? stun)
                    || !e.TryGetComponent(out IDamageableComponent? damage)
                    || !e.TryGetComponent(out IPhysicsComponent? otherBody))
                    continue;

                var percentage = otherBody.WorldAABB.IntersectPercentage(body.WorldAABB);

                if (percentage < 0.1f)
                    continue;

                damage.ChangeDamage(DamageType.Blunt, DoorCrushDamage, false, Owner);
                stun.Paralyze(DoorStunTime);

                // If we hit someone, open up after stun (opens right when stun ends)
                Timer.Spawn(TimeSpan.FromSeconds(DoorStunTime) - OpenTimeOne - OpenTimeTwo, Open);
                break;
            }
        }

        public bool IsHoldingPressure(float threshold = 20)
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos))
                return false;

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.GridID);

            if (gridAtmosphere == null)
                return false;

            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            foreach (var (direction, adjacent) in gridAtmosphere.GetAdjacentTiles(tileAtmos.GridIndices))
            {
                var moles = adjacent.Air.TotalMoles;
                if (moles < minMoles)
                    minMoles = moles;
                if (moles > maxMoles)
                    maxMoles = moles;
            }

            return (maxMoles - minMoles) > threshold;
        }

        public bool IsHoldingFire()
        {
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos))
                return false;

            if (tileAtmos.Hotspot.Valid)
                return true;

            var gridAtmosphere = atmosphereSystem.GetGridAtmosphere(Owner.Transform.GridID);

            if (gridAtmosphere == null)
                return false;

            foreach (var (direction, adjacent) in gridAtmosphere.GetAdjacentTiles(tileAtmos.GridIndices))
            {
                if (adjacent.Hotspot.Valid)
                    return true;
            }

            return false;
        }

        public bool Close()
        {
            bool shouldCheckCrush = false;
            if (Owner.TryGetComponent(out IPhysicsComponent? physics))
                physics.CanCollide = true;

            if (_canCrush && physics != null &&
                physics.IsColliding(Vector2.Zero, false))
            {
                if (Safety)
                {
                    physics.CanCollide = false;
                    return false;
                }

                // check if we crush someone while closing
                shouldCheckCrush = true;
            }

            State = DoorState.Closing;
            OpenTimeCounter = 0;
            SetAppearance(DoorVisualState.Closing);
            if (_occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = true;
            }

            Timer.Spawn(CloseTimeOne, async () =>
            {
                if (shouldCheckCrush && _canCrush)
                {
                    CheckCrush();
                }

                if (Owner.TryGetComponent(out AirtightComponent? airtight))
                {
                    airtight.AirBlocked = true;
                }

                if (Owner.TryGetComponent(out IPhysicsComponent? body))
                {
                    body.CanCollide = true;
                }

                await Timer.Delay(CloseTimeTwo, _cancellationTokenSource.Token);

                _canWeldShut = true;
                State = DoorState.Closed;
                SetAppearance(DoorVisualState.Closed);
            }, _cancellationTokenSource.Token);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, true));
            return true;
        }

        public virtual void Deny()
        {
            if (State == DoorState.Open || _isWeldedShut)
                return;

            SetAppearance(DoorVisualState.Deny);
            Timer.Spawn(DenyTime, () =>
            {
                SetAppearance(DoorVisualState.Closed);
            }, _cancellationTokenSource.Token);
        }

        public virtual void OnUpdate(float frameTime)
        {
            if (State != DoorState.Open)
            {
                return;
            }

            if (AutoClose)
            {
                OpenTimeCounter += frameTime;
            }

            if (OpenTimeCounter > CloseSpeed)
            {
                if (!CanClose() || !Close())
                {
                    // Try again in 2 seconds if it's jammed or something.
                    OpenTimeCounter -= 2;
                }
            }
        }

        public enum DoorState
        {
            Closed,
            Open,
            Closing,
            Opening,
        }

        public virtual async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_canWeldShut)
            {
                _beingWelded = false;
                return false;
            }

            if (!eventArgs.Using.TryGetComponent(out WelderComponent? tool) || !tool.WelderLit)
            {
                _beingWelded = false;
                return false;
            }

            if (_beingWelded)
                return false;

            _beingWelded = true;

            if (!await tool.UseTool(eventArgs.User, Owner, 3f, ToolQuality.Welding, 3f, () => _canWeldShut))
            {
                _beingWelded = false;
                return false;
            }

            _beingWelded = false;
            IsWeldedShut ^= true;
            return true;
        }
    }

    public sealed class DoorStateMessage : EntitySystemMessage
    {
        public ServerDoorComponent Component { get; }
        public ServerDoorComponent.DoorState State { get; }

        public DoorStateMessage(ServerDoorComponent component, ServerDoorComponent.DoorState state)
        {
            Component = component;
            State = state;
        }
    }
}
