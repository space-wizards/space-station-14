#nullable enable
using System;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent, IInteractHand, IDragDrop
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        /// <summary>
        ///     The amount of space that this entity occupies in a <see cref="StrapComponent"/>.
        /// </summary>
        private int _size;

        /// <summary>
        ///     The range from which this entity can buckle to a <see cref="StrapComponent"/>.
        /// </summary>
        private float _range;

        /// <summary>
        ///     The amount of time that must pass for this entity to
        ///     be able to unbuckle after recently buckling.
        /// </summary>
        private TimeSpan _unbuckleDelay;

        /// <summary>
        ///     The time that this entity buckled at.
        /// </summary>
        private TimeSpan _buckleTime;

        private StrapComponent? _buckledTo;

        [ViewVariables]
        public StrapComponent? BuckledTo
        {
            get => _buckledTo;
            private set
            {
                _buckledTo = value;
                _buckleTime = _gameTiming.CurTime;
                Dirty();
            }
        }

        [ViewVariables]
        protected override bool Buckled => BuckledTo != null;

        public bool ContainerChanged { get; private set; }

        [ViewVariables]
        public int Size => _size;

        private void BuckleStatus()
        {
            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Buckled,
                    Buckled
                        ? BuckledTo!.BuckledIcon
                        : "/Textures/Interface/StatusEffects/Buckle/unbuckled.png");
            }
        }

        private void ReAttach(StrapComponent strap)
        {
            var ownTransform = Owner.Transform;
            var strapTransform = strap.Owner.Transform;

            ownTransform.GridPosition = strapTransform.GridPosition;
            ownTransform.AttachParent(strapTransform);

            switch (strap.Position)
            {
                case StrapPosition.None:
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Stand:
                    StandingStateHelper.Standing(Owner);
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Down:
                    StandingStateHelper.Down(Owner);
                    ownTransform.WorldRotation = Angle.South;
                    break;
            }
        }

        private bool TryBuckle(IEntity user, IEntity to)
        {
            if (user == null || user == to)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(user))
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You can't do that!"));
                return false;
            }

            if (!to.TryGetComponent(out StrapComponent strap))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't buckle yourself there!"
                        : "You can't buckle {0:them} there!", Owner));
                return false;
            }

            var strapPosition = strap.Owner.Transform.MapPosition;

            if (!InteractionChecks.InRangeUnobstructed(user, strapPosition, _range))
            {
                return false;
            }

            if (ContainerHelpers.TryGetContainer(Owner, out var ownerContainer))
            {
                if (!ContainerHelpers.TryGetContainer(strap.Owner, out var strapContainer) ||
                    ownerContainer != strapContainer)
                {
                    _notifyManager.PopupMessage(strap.Owner, user,
                        Loc.GetString("You can't reach there!"));
                    return false;
                }
            }

            if (!user.HasComponent<HandsComponent>())
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You don't have hands!"));
                return false;
            }

            if (Buckled)
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You are already buckled in!"
                        : "{0:They} are already buckled in!", Owner));
                return false;
            }

            var parent = to.Transform.Parent;
            while (parent != null)
            {
                if (parent == user.Transform)
                {
                    _notifyManager.PopupMessage(Owner, user,
                        Loc.GetString(Owner == user
                            ? "You can't buckle yourself there!"
                            : "You can't buckle {0:them} there!", Owner));
                    return false;
                }

                parent = parent.Parent;
            }

            if (!strap.HasSpace(this))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't fit there!"
                        : "{0:They} can't fit there!", Owner));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(strap.BuckleSound, Owner);

            if (!strap.TryAdd(this))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString(Owner == user
                        ? "You can't buckle yourself there!"
                        : "You can't buckle {0:them} there!", Owner));
                return false;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, true);
            }

            ReAttach(strap);

            BuckledTo = strap;
            BuckleStatus();

            return true;
        }

        public bool TryUnbuckle(IEntity user, bool force = false)
        {
            if (BuckledTo == null)
            {
                return false;
            }

            if (!force)
            {
                if (_gameTiming.CurTime < _buckleTime + _unbuckleDelay)
                {
                    return false;
                }

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    _notifyManager.PopupMessage(user, user,
                        Loc.GetString("You can't do that!"));
                    return false;
                }

                var strapPosition = Owner.Transform.MapPosition;

                if (!InteractionChecks.InRangeUnobstructed(user, strapPosition, _range))
                {
                    _notifyManager.PopupMessage(Owner, user,
                        Loc.GetString("You can't reach there!"));
                    return false;
                }
            }

            if (BuckledTo.Owner.TryGetComponent(out StrapComponent strap))
            {
                strap.Remove(this);
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(strap.UnbuckleSound, Owner);
            }

            if (Owner.Transform.Parent == BuckledTo.Owner.Transform)
            {
                Owner.Transform.DetachParent();
                Owner.Transform.WorldRotation = BuckledTo.Owner.Transform.WorldRotation;
            }

            BuckledTo = null;

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, false);
            }

            if (Owner.TryGetComponent(out StunnableComponent stunnable) && stunnable.KnockedDown)
            {
                StandingStateHelper.Down(Owner);
            }
            else
            {
                StandingStateHelper.Standing(Owner);
            }

            if (Owner.TryGetComponent(out SpeciesComponent species))
            {
                species.CurrentDamageState.EnterState(Owner);
            }

            BuckleStatus();

            return true;
        }

        public bool ToggleBuckle(IEntity user, IEntity to)
        {
            if (BuckledTo?.Owner == to)
            {
                return TryUnbuckle(user);
            }

            return TryBuckle(user, to);
        }

        private void InsertIntoContainer(ContainerModifiedMessage message)
        {
            if (message.Entity != Owner)
            {
                return;
            }

            ContainerChanged = true;
        }

        public void Update()
        {
            if (!ContainerChanged || BuckledTo == null)
            {
                return;
            }

            var contained = ContainerHelpers.TryGetContainer(Owner, out var ownContainer);
            var strapContained = ContainerHelpers.TryGetContainer(BuckledTo.Owner, out var strapContainer);

            if (contained != strapContained || ownContainer != strapContainer)
            {
                TryUnbuckle(Owner, true);
                return;
            }

            if (!contained && !strapContained)
            {
                ReAttach(BuckledTo);
            }

            ContainerChanged = false;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _size, "size", 100);
            serializer.DataField(ref _range, "range", SharedInteractionSystem.InteractionRange / 2);

            var seconds = 0.25f;
            serializer.DataField(ref seconds, "cooldown", 0.25f);

            _unbuckleDelay = TimeSpan.FromSeconds(seconds);
        }

        public override void Initialize()
        {
            base.Initialize();

            _entityManager.EventBus.SubscribeEvent<EntInsertedIntoContainerMessage>(EventSource.Local, this, InsertIntoContainer);
            _entityManager.EventBus.SubscribeEvent<EntRemovedFromContainerMessage>(EventSource.Local, this, InsertIntoContainer);
        }

        protected override void Startup()
        {
            base.Startup();
            BuckleStatus();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _entityManager.EventBus.UnsubscribeEvents(this);

            if (BuckledTo != null &&
                BuckledTo.Owner.TryGetComponent(out StrapComponent strap))
            {
                strap.Remove(this);
            }

            TryUnbuckle(Owner, true);

            _buckleTime = default;
            BuckleStatus();
        }

        public override ComponentState GetComponentState()
        {
            int? drawDepth = null;

            if (BuckledTo != null &&
                Owner.Transform.WorldRotation.GetCardinalDir() == Direction.North &&
                BuckledTo.Owner.TryGetComponent(out SpriteComponent strapSprite))
            {
                drawDepth = strapSprite.DrawDepth - 1;
            }

            return new BuckleComponentState(Buckled, drawDepth);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle(eventArgs.User);
        }

        bool IDragDrop.DragDrop(DragDropEventArgs eventArgs)
        {
            return TryBuckle(eventArgs.User, eventArgs.Target);
        }

        [Verb]
        private sealed class BuckleVerb : Verb<BuckleComponent>
        {
            protected override void GetData(IEntity user, BuckleComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || !component.Buckled)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, BuckleComponent component)
            {
                component.TryUnbuckle(user);
            }
        }
    }
}
