using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Alert;
using Content.Server.Hands.Components;
using Content.Server.Pulling;
using Content.Server.Stunnable.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Buckle.Components
{
    /// <summary>
    ///     Component that handles sitting entities into <see cref="StrapComponent"/>s.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public class BuckleComponent : SharedBuckleComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ComponentDependency] public readonly AppearanceComponent? Appearance = null;
        [ComponentDependency] private readonly ServerAlertsComponent? _serverAlerts = null;
        [ComponentDependency] private readonly MobStateComponent? _mobState = null;

        [DataField("size")]
        private int _size = 100;

        /// <summary>
        ///     The amount of time that must pass for this entity to
        ///     be able to unbuckle after recently buckling.
        /// </summary>
        [DataField("delay")]
        [ViewVariables]
        private TimeSpan _unbuckleDelay = TimeSpan.FromSeconds(0.25f);

        /// <summary>
        ///     The time that this entity buckled at.
        /// </summary>
        [ViewVariables]
        private TimeSpan _buckleTime;

        /// <summary>
        ///     The position offset that is being applied to this entity if buckled.
        /// </summary>
        public Vector2 BuckleOffset { get; private set; }

        private StrapComponent? _buckledTo;

        /// <summary>
        ///     The strap that this component is buckled to.
        /// </summary>
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
        public override bool Buckled => BuckledTo != null;

        /// <summary>
        ///     The amount of space that this entity occupies in a
        ///     <see cref="StrapComponent"/>.
        /// </summary>
        [ViewVariables]
        public int Size => _size;

        /// <summary>
        ///     Shows or hides the buckled status effect depending on if the
        ///     entity is buckled or not.
        /// </summary>
        private void UpdateBuckleStatus()
        {
            if (_serverAlerts == null)
            {
                return;
            }

            if (Buckled)
            {
                _serverAlerts.ShowAlert(BuckledTo?.BuckledAlertType ?? AlertType.Buckled);
            }
            else
            {
                _serverAlerts.ClearAlertCategory(AlertCategory.Buckled);
            }
        }

        /// <summary>
        ///     Reattaches this entity to the strap, modifying its position and rotation.
        /// </summary>
        /// <param name="strap">The strap to reattach to.</param>
        public void ReAttach(StrapComponent strap)
        {
            var ownTransform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid);
            var strapTransform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(strap.Owner.Uid);

            ownTransform.AttachParent(strapTransform);
            ownTransform.LocalRotation = Angle.Zero;

            switch (strap.Position)
            {
                case StrapPosition.None:
                    break;
                case StrapPosition.Stand:
                    EntitySystem.Get<StandingStateSystem>().Stand(Owner.Uid);
                    break;
                case StrapPosition.Down:
                    EntitySystem.Get<StandingStateSystem>().Down(Owner.Uid, false, false);
                    break;
            }

            ownTransform.LocalPosition = Vector2.Zero + BuckleOffset;
        }

        public bool CanBuckle(IEntity? user, IEntity to, [NotNullWhen(true)] out StrapComponent? strap)
        {
            strap = null;

            if (user == null || user == to)
            {
                return false;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user.Uid))
            {
                user.PopupMessage(Loc.GetString("buckle-component-cannot-do-that-message"));
                return false;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(to.Uid, out strap))
            {
                return false;
            }

            var component = strap;
            bool Ignored(IEntity entity) => entity == Owner || entity == user || entity == component.Owner;

            if (!Owner.InRangeUnobstructed(strap, Range, predicate: Ignored, popup: true))
            {
                return false;
            }

            // If in a container
            if (Owner.TryGetContainer(out var ownerContainer))
            {
                // And not in the same container as the strap
                if (!strap.Owner.TryGetContainer(out var strapContainer) ||
                    ownerContainer != strapContainer)
                {
                    return false;
                }
            }

            if (!IoCManager.Resolve<IEntityManager>().HasComponent<HandsComponent>(user.Uid))
            {
                user.PopupMessage(Loc.GetString("buckle-component-no-hands-message "));
                return false;
            }

            if (Buckled)
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-already-buckled-message"
                    : "buckle-component-other-already-buckled-message", ("owner", Owner));
                Owner.PopupMessage(user, message);

                return false;
            }

            var parent = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(to.Uid).Parent;
            while (parent != null)
            {
                if (parent == IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(user.Uid))
                {
                    var message = Loc.GetString(Owner == user
                        ? "buckle-component-cannot-buckle-message"
                        : "buckle-component-other-cannot-buckle-message", ("owner", Owner));
                    Owner.PopupMessage(user, message);

                    return false;
                }

                parent = parent.Parent;
            }

            if (!strap.HasSpace(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-cannot-fit-message"
                    : "buckle-component-other-cannot-fit-message", ("owner", Owner));
                Owner.PopupMessage(user, message);

                return false;
            }

            return true;
        }

        public override bool TryBuckle(IEntity? user, IEntity to)
        {
            if (user == null || !CanBuckle(user, to, out var strap))
            {
                return false;
            }

            SoundSystem.Play(Filter.Pvs(Owner), strap.BuckleSound.GetSound(), Owner);

            if (!strap.TryAdd(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message", ("owner", Owner));
                Owner.PopupMessage(user, message);
                return false;
            }

            Appearance?.SetData(BuckleVisuals.Buckled, true);

            ReAttach(strap);

            BuckledTo = strap;
            LastEntityBuckledTo = BuckledTo.Owner.Uid;
            DontCollide = true;

            UpdateBuckleStatus();

#pragma warning disable 618
            SendMessage(new BuckleMessage(Owner, to));
#pragma warning restore 618

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out SharedPullableComponent? ownerPullable))
            {
                if (ownerPullable.Puller != null)
                {
                    EntitySystem.Get<PullingSystem>().TryStopPull(ownerPullable);
                }
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(to.Uid, out SharedPullableComponent? toPullable))
            {
                if (toPullable.Puller == Owner)
                {
                    // can't pull it and buckle to it at the same time
                    EntitySystem.Get<PullingSystem>().TryStopPull(toPullable);
                }
            }

            return true;
        }

        /// <summary>
        ///     Tries to unbuckle the Owner of this component from its current strap.
        /// </summary>
        /// <param name="user">The entity doing the unbuckling.</param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not. Does not guarantee true to
        ///     be returned, but guarantees the owner to be unbuckled afterwards.
        /// </param>
        /// <returns>
        ///     true if the owner was unbuckled, otherwise false even if the owner
        ///     was previously already unbuckled.
        /// </returns>
        public bool TryUnbuckle(IEntity user, bool force = false)
        {
            if (BuckledTo == null)
            {
                return false;
            }

            var oldBuckledTo = BuckledTo;

            if (!force)
            {
                if (_gameTiming.CurTime < _buckleTime + _unbuckleDelay)
                {
                    return false;
                }

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user.Uid))
                {
                    user.PopupMessage(Loc.GetString("buckle-component-cannot-do-that-message"));
                    return false;
                }

                if (!user.InRangeUnobstructed(oldBuckledTo, Range, popup: true))
                {
                    return false;
                }
            }

            BuckledTo = null;

            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Parent == IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(oldBuckledTo.Owner.Uid))
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).AttachParentToContainerOrGrid();
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).WorldRotation = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(oldBuckledTo.Owner.Uid).WorldRotation;
            }

            Appearance?.SetData(BuckleVisuals.Buckled, false);

            if (IoCManager.Resolve<IEntityManager>().HasComponent<KnockedDownComponent>(Owner.Uid)
                || (_mobState?.IsIncapacitated() ?? false))
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner.Uid);
            }
            else
            {
                EntitySystem.Get<StandingStateSystem>().Stand(Owner.Uid);
            }

            _mobState?.CurrentState?.EnterState(Owner.Uid, IoCManager.Resolve<IEntityManager>());

            UpdateBuckleStatus();

            oldBuckledTo.Remove(this);
            SoundSystem.Play(Filter.Pvs(Owner), oldBuckledTo.UnbuckleSound.GetSound(), Owner);

#pragma warning disable 618
            SendMessage(new UnbuckleMessage(Owner, oldBuckledTo.Owner));
#pragma warning restore 618

            return true;
        }

        /// <summary>
        ///     Makes an entity toggle the buckling status of the owner to a
        ///     specific entity.
        /// </summary>
        /// <param name="user">The entity doing the buckling/unbuckling.</param>
        /// <param name="to">
        ///     The entity to toggle the buckle status of the owner to.
        /// </param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not, if it happens. Does not
        ///     guarantee true to be returned, but guarantees the owner to be
        ///     unbuckled afterwards.
        /// </param>
        /// <returns>true if the buckling status was changed, false otherwise.</returns>
        public bool ToggleBuckle(IEntity user, IEntity to, bool force = false)
        {
            if (BuckledTo?.Owner == to)
            {
                return TryUnbuckle(user, force);
            }

            return TryBuckle(user, to);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateBuckleStatus();
        }

        protected override void Shutdown()
        {
            BuckledTo?.Remove(this);
            TryUnbuckle(Owner, true);

            _buckleTime = default;
            UpdateBuckleStatus();

            base.Shutdown();
        }

        public override ComponentState GetComponentState()
        {
            int? drawDepth = null;

            if (BuckledTo != null &&
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(BuckledTo.Owner.Uid).LocalRotation.GetCardinalDir() == Direction.North &&
                BuckledTo.SpriteComponent != null)
            {
                drawDepth = BuckledTo.SpriteComponent.DrawDepth - 1;
            }

            return new BuckleComponentState(Buckled, drawDepth, LastEntityBuckledTo, DontCollide);
        }

        public void Update(PhysicsComponent physics)
        {
            if (!DontCollide)
                return;

            physics.WakeBody();

            if (!IsOnStrapEntityThisFrame && DontCollide)
            {
                DontCollide = false;
                TryUnbuckle(Owner);
                Dirty();
            }

            IsOnStrapEntityThisFrame = false;
        }
    }
}
