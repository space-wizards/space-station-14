using System;
using System.Linq;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ServerDoorComponent : Component, IActivate, ICollideBehavior
    {
        public override string Name => "Door";

        private DoorState _state = DoorState.Closed;

        protected virtual DoorState State
        {
            get => _state;
            set => _state = value;
        }

        protected float OpenTimeCounter;
        protected bool AutoClose = true;
        protected const float AutoCloseDelay = 5;
        protected float CloseSpeed = AutoCloseDelay;

        private AirtightComponent airtightComponent;
        private ICollidableComponent _collidableComponent;
        private AppearanceComponent _appearance;
        private CancellationTokenSource _cancellationTokenSource;

        private static readonly TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.3f);
        private static readonly TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.9f);
        private static readonly TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.3f);
        private static readonly TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.9f);
        private static readonly TimeSpan DenyTime = TimeSpan.FromSeconds(0.45f);

        private const int DoorCrushDamage = 15;
        private const float DoorStunTime = 5f;
        protected bool Safety = true;

        [ViewVariables]
        private bool _occludes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _occludes, "occludes", true);
        }

        public override void Initialize()
        {
            base.Initialize();

            airtightComponent = Owner.GetComponent<AirtightComponent>();
            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override void OnRemove()
        {
            _cancellationTokenSource.Cancel();
            _collidableComponent = null;
            _appearance = null;

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
            if (entity.HasComponent(typeof(SpeciesComponent)))
            {
                if (!entity.TryGetComponent<IMoverComponent>(out var mover)) return;

                // TODO: temporary hack to fix the physics system raising collision events akwardly.
                // E.g. when moving parallel to a door by going off the side of a wall.
                var (walking, sprinting) = mover.VelocityDir;
                // Also TODO: walking and sprint dir are added together here
                // instead of calculating their contribution correctly.
                var dotProduct = Vector2.Dot((sprinting + walking).Normalized, (entity.Transform.WorldPosition - Owner.Transform.WorldPosition).Normalized);
                if (dotProduct <= -0.9f)
                    TryOpen(entity);
            }
        }

        private void SetAppearance(DoorVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(DoorVisuals.VisualState, state);
        }

        public virtual bool CanOpen()
        {
            return true;
        }

        public bool CanOpen(IEntity user)
        {
            if (!CanOpen()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
            {
                return true;
            }
            return accessReader.IsAllowed(user);
        }

        public void TryOpen(IEntity user)
        {
            if (!CanOpen(user))
            {
                Deny();
                return;
            }
            Open();
        }

        public void Open()
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            State = DoorState.Opening;
            SetAppearance(DoorVisualState.Opening);
            if (_occludes && Owner.TryGetComponent(out OccluderComponent occluder))
            {
                occluder.Enabled = false;
            }

            Timer.Spawn(OpenTimeOne, async () =>
            {
                airtightComponent.AirBlocked = false;
                _collidableComponent.Hard = false;

                await Timer.Delay(OpenTimeTwo, _cancellationTokenSource.Token);

                State = DoorState.Open;
                SetAppearance(DoorVisualState.Open);
            }, _cancellationTokenSource.Token);

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner.Uid, false));
        }

        public virtual bool CanClose()
        {
            return true;
        }

        public bool CanClose(IEntity user)
        {
            if (!CanClose()) return false;
            if (!Owner.TryGetComponent(out AccessReader accessReader))
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
            // Check if collides with something
            var collidesWith = _collidableComponent.GetCollidingEntities(Vector2.Zero, false);
            if (collidesWith.Count() != 0)
            {
                // Crush
                bool hitSomeone = false;
                foreach (var e in collidesWith)
                {
                    if (!e.TryGetComponent(out StunnableComponent stun)
                        || !e.TryGetComponent(out DamageableComponent damage)
                        || !e.TryGetComponent(out ICollidableComponent otherBody)
                        || !Owner.TryGetComponent(out ICollidableComponent body))
                        continue;

                    var percentage = otherBody.WorldAABB.IntersectPercentage(body.WorldAABB);

                    if (percentage < 0.1f)
                        continue;

                    damage.TakeDamage(Shared.GameObjects.DamageType.Brute, DoorCrushDamage);
                    stun.Paralyze(DoorStunTime);
                    hitSomeone = true;
                }
                // If we hit someone, open up after stun (opens right when stun ends)
                if (hitSomeone)
                {
                    Timer.Spawn(TimeSpan.FromSeconds(DoorStunTime) - OpenTimeOne - OpenTimeTwo, () => Open());
                }
            }
        }

        public bool Close()
        {
            bool shouldCheckCrush = false;
            if (_collidableComponent.IsColliding(Vector2.Zero, false))
            {
                if (Safety)
                    return false;

                // check if we crush someone while closing
                shouldCheckCrush = true;
            }

            State = DoorState.Closing;
            OpenTimeCounter = 0;
            SetAppearance(DoorVisualState.Closing);
            if (_occludes && Owner.TryGetComponent(out OccluderComponent occluder))
            {
                occluder.Enabled = true;
            }

            Timer.Spawn(CloseTimeOne, async () =>
            {
                if (shouldCheckCrush)
                {
                    CheckCrush();
                }

                airtightComponent.AirBlocked = true;
                _collidableComponent.Hard = true;

                await Timer.Delay(CloseTimeTwo, _cancellationTokenSource.Token);

                State = DoorState.Closed;
                SetAppearance(DoorVisualState.Closed);
            }, _cancellationTokenSource.Token);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner.Uid, true));
            return true;
        }

        public virtual void Deny()
        {
            if (State == DoorState.Open)
            {
                return;
            }

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

        protected enum DoorState
        {
            Closed,
            Open,
            Closing,
            Opening,
        }
    }
}
