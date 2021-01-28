#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components
{
    //TODO: Write documentation
    [RegisterComponent]
    [ComponentReference(typeof(SharedEntityInputComponent))]
    [ComponentReference(typeof(IInteractUsing))]
    [ComponentReference(typeof(IThrowCollide))]
    public class EntityInputComponent : SharedEntityInputComponent, IInteractUsing, IThrowCollide
    {
        [Dependency]
        private readonly IGameTiming _gameTiming = default!;

        [Dependency] readonly IRobustRandom _random = default!;

        [ViewVariables]
        public IReadOnlyList<IEntity> ContainedEntities => _container != null  ? _container.ContainedEntities : new List<IEntity>();

        /// <summary>
        ///     The delay for an entity trying to move out of this unit.
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit.
        /// </summary>
        [ViewVariables]
        private TimeSpan _lastExitAttempt;

        private string _containerID = "";

        /// <summary>
        ///     Container of entities inside this entity input.
        /// </summary>
        [ViewVariables]
        private Container _container = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _entryDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _containerID, "containerName", Name);
            serializer.DataField(ref _entryDelay, "entryDelay", 0.5f);
        }

        public override void OnRemove()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                _container.ForceRemove(entity);
            }

            _container = null!;

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (!msg.Entity.TryGetComponent(out HandsComponent? hands) || hands.Count == 0 || _gameTiming.CurTime < _lastExitAttempt + ExitAttemptDelay)
                    {
                        break;
                    }

                    _lastExitAttempt = _gameTiming.CurTime;
                    Remove(msg.Entity);
                    break;

                case EjectInputContentsMessage:
                    TryEjectContents();
                    break;
            }
        }

        public async Task<bool> TryInsert(IEntity entity, IEntity? user = default)
        {
            if (!CanInsert(entity))
                return false;

            if (user != null && _entryDelay > 0f)
            {
                var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                var doAfterArgs = new DoAfterEventArgs(user, _entryDelay, default, Owner)
                {
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = false,
                };

                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;

            }

            SendMessage(new EntityInsertetMessage(entity));
            return _container.Insert(entity);
        }

        public override bool CanInsert(IEntity entity)
        {
            return base.CanInsert(entity) && _container.CanInsert(entity);
        }

        protected override void Startup()
        {
            base.Startup();

            if (_container == default!)
            {
                _container = ContainerManagerComponent.Ensure<Container>(_containerID, Owner);
            }
        }

        private bool TryDrop(IEntity user, IEntity entity)
        {
            if (!user.TryGetComponent(out HandsComponent? hands))
            {
                return false;
            }

            if (!CanInsert(entity) || !hands.Drop(entity, _container))
            {
                return false;
            }

            SendMessage(new EntityInsertetMessage(entity));
            return true;
        }

        private void Remove(IEntity entity)
        {
            _container.Remove(entity);
        }

        private void TryEjectContents()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                Remove(entity);
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryDrop(eventArgs.User, eventArgs.Using);
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            _ = TryInsert(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        void IThrowCollide.HitBy(ThrowCollideEventArgs eventArgs)
        {
            if (!CanInsert(eventArgs.Thrown) || _random.NextDouble() > 0.75 || !_container.Insert(eventArgs.Thrown))
            {
                return;
            }

            SendMessage(new EntityInsertetMessage(eventArgs.Thrown));
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<EntityInputComponent>
        {
            protected override void GetData(IEntity user, EntityInputComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.ContainedEntities.Contains(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Jump inside");
            }

            protected override void Activate(IEntity user, EntityInputComponent component)
            {
                _ = component.TryInsert(user, user);
            }
        }

        [Verb]
        private sealed class EjectContentsVerb : Verb<EntityInputComponent>
        {
            protected override void GetData(IEntity user, EntityInputComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.ContainedEntities.Contains(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Eject contents");
            }

            protected override void Activate(IEntity user, EntityInputComponent component)
            {
                component.TryEjectContents();
            }
        }

        public class EjectInputContentsMessage : ComponentMessage {}

        public class EntityInsertetMessage : ComponentMessage
        {
            public IEntity Entity { get; }

            public EntityInsertetMessage(IEntity entity)
            {
                Entity = entity;
            }
        }
    }
}
