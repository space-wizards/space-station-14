using System;
using System.Linq;
using System.Threading;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalUnitComponent : SharedDisposalUnitComponent, IInteractHand, IInteractUsing
    {
        public override string Name => "DisposalUnit";

        /// <summary>
        ///     The delay for an entity trying to move out of this unit
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit
        /// </summary>
        private TimeSpan _lastExitAttempt;

        /// <summary>
        ///     The time that it takes this disposal unit to flush its contents
        /// </summary>
        [ViewVariables]
        private TimeSpan _flushTime;

        /// <summary>
        ///     Token used to cancel delayed appearance changes
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        ///     Container of entities inside this disposal unit
        /// </summary>
        [ViewVariables]
        private Container _container;

        private bool CanInsert(IEntity entity)
        {
            return entity.HasComponent<DisposableComponent>() &&
                   _container.CanInsert(entity);
        }

        public bool TryInsert(IEntity entity)
        {
            return CanInsert(entity) && _container.Insert(entity);
        }

        private bool Remove(IEntity entity)
        {
            return _container.Remove(entity);
        }

        private bool CanFlush()
        {
            return (!Owner.TryGetComponent(out PowerReceiverComponent receiver) ||
                    receiver.Powered) &&
                   (!Owner.TryGetComponent(out PhysicsComponent physics) ||
                    physics.Anchored);
        }

        private bool TryFlush()
        {
            if (!CanFlush())
            {
                return false;
            }

            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var entry = snapGrid
                .GetLocal()
                .FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entry == null)
            {
                return false;
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();
            foreach (var entity in _container.ContainedEntities.ToList())
            {
                _container.Remove(entity);
                entryComponent.TryInsert(entity);
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalUnitVisuals.VisualState, DisposalUnitVisualState.Flushing);

                Timer.Spawn(_flushTime, AnchoredChanged, _cancellationTokenSource.Token);
            }

            return true;
        }

        private void AnchoredChanged()
        {
            var physics = Owner.GetComponent<PhysicsComponent>();

            if (physics.Anchored)
            {
                Anchored();
            }
            else
            {
                UnAnchored();
            }
        }

        private void Anchored()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalUnitVisuals.VisualState, DisposalUnitVisualState.Anchored);
            }
        }

        private void UnAnchored()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalUnitVisuals.VisualState, DisposalUnitVisualState.UnAnchored);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var flushSeconds = 2;
            serializer.DataField(ref flushSeconds, "flushTime", 2);

            _flushTime = TimeSpan.FromSeconds(flushSeconds);
        }

        public override void Initialize()
        {
            base.Initialize();

            _container = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponent<AnchorableComponent>();
            var physics = Owner.EnsureComponent<PhysicsComponent>();

            physics.AnchoredChanged += AnchoredChanged;
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                var anchored = Owner.GetComponent<PhysicsComponent>().Anchored;
                appearance.SetData(DisposalVisuals.Anchored, anchored);
            }
        }

        public override void OnRemove()
        {
            _cancellationTokenSource.Cancel();
            _container = null;

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    var timing = IoCManager.Resolve<IGameTiming>();
                    if (!msg.Entity.HasComponent<HandsComponent>() ||
                        timing.CurTime < _lastExitAttempt + ExitAttemptDelay)
                    {
                        break;
                    }

                    _lastExitAttempt = timing.CurTime;
                    Remove(msg.Entity);
                    break;
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryFlush();
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsert(eventArgs.Using);
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Jump inside");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryInsert(user);
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Flush");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryFlush();
            }
        }
    }
}
