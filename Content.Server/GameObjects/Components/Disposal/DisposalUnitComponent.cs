using System;
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalUnitComponent : Component, IInteractHand, IInteractUsing
    {
        /// <summary>
        ///     The delay for an entity trying to move out of this unit
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        private TimeSpan _lastExitAttempt;

        /// <summary>
        ///     The sound file that is played when this unit flushes its contents
        /// </summary>
        private string _flushSound;

        [ViewVariables] private Container _container;

        public override string Name => "DisposalUnit";

        private bool Functional()
        {
            return !Owner.TryGetComponent(out PowerReceiverComponent receiver) ||
                   receiver.Powered;
        }

        private bool TryInsert(IEntity entity)
        {
            // TODO: Click drag
            return _container.Insert(entity);
        }

        private bool Remove(IEntity entity)
        {
            return _container.Remove(entity);
        }

        private bool TryFlush()
        {
            if (!Functional())
            {
                return false;
            }

            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var entry = snapGrid
                .GetLocal()
                .FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entry == null)
            {
                return false; // TODO connections
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();
            foreach (var entity in _container.ContainedEntities.ToList())
            {
                _container.Remove(entity);
                entryComponent.TryInsert(entity);
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords(_flushSound, Owner.Transform.GridPosition);

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
                appearance.SetData(DisposalVisuals.Anchored, true);
            }
        }

        private void UnAnchored()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalVisuals.Anchored, false);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _flushSound, "flushSound", "/Audio/machines/disposalflush.ogg");
        }

        public override void Initialize()
        {
            base.Initialize();

            _container = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            Owner.EnsureComponent<AnchorableComponent>();

            var physics = Owner.EnsureComponent<PhysicsComponent>();

            physics.AnchoredChanged += AnchoredChanged;
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                var anchored = Owner.GetComponent<PhysicsComponent>().Anchored;
                appearance.SetData(DisposalVisuals.Anchored, anchored);
            }
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
