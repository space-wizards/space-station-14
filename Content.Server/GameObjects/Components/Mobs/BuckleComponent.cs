using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent, IActionBlocker, IInteractHand
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystem;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        [CanBeNull] private IEntity _buckledTo;

        [ViewVariables]
        public IEntity BuckledTo
        {
            get => _buckledTo;
            private set
            {
                var old = value ?? _buckledTo;
                _buckledTo = value;

                if (old != null && old.TryGetComponent(out StrapComponent strap))
                {
                    if (value == null)
                    {
                        strap.RemoveEntity(Owner);
                    }
                    else
                    {
                        strap.AddEntity(Owner);
                    }
                }
            }
        }

        private void BuckleStatus()
        {
            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Buckled,
                    _buckledTo == null
                        ? "/Textures/Mob/UI/Buckle/unbuckled.png"
                        : "/Textures/Mob/UI/Buckle/buckled.png");
            }
        }

        private bool TryBuckle(IEntity user, IEntity to)
        {
            if (user == null)
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
            var range = SharedInteractionSystem.InteractionRange / 2;

            if (!InteractionChecks.InRangeUnobstructed(user, strapPosition, range))
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You can't reach there!"));
                return false;
            }

            if (!user.TryGetComponent(out HandsComponent hands))
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You don't have hands!"));
                return false;
            }

            if (hands.GetActiveHand != null)
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("Your hand isn't free!"));
                return false;
            }

            if (_buckledTo != null)
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString("{0:They} are already buckled in!", Owner));
                return false;
            }

            if (!to.TryGetComponent(out StrapComponent strap))
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString("You can't buckle {0:them} there!", Owner));
                return false;
            }

            if (strap.BuckledEntity != null)
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString("Someone is already buckled there!"));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(strap.BuckleSound, Owner, AudioParams.Default.WithVolume(-2f));
            BuckledTo = strap.Owner;

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, true);
            }

            var ownTransform = Owner.Transform;
            var closestTransform = strap.Owner.Transform;

            ownTransform.GridPosition = closestTransform.GridPosition;
            ownTransform.AttachParent(closestTransform);

            switch (strap.Position)
            {
                case StrapPosition.Stand:
                    StandingStateHelper.Standing(Owner);
                    ownTransform.WorldRotation = closestTransform.WorldRotation;
                    break;
                case StrapPosition.Down:
                    StandingStateHelper.Down(Owner);
                    ownTransform.WorldRotation = Angle.South;
                    break;
            }

            BuckleStatus();

            return true;
        }

        public bool TryUnbuckle(IEntity user)
        {
            if (_buckledTo == null)
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
            var range = SharedInteractionSystem.InteractionRange / 2;

            if (!InteractionChecks.InRangeUnobstructed(user, strapPosition, range))
            {
                _notifyManager.PopupMessage(user, user,
                    Loc.GetString("You can't reach there!"));
                return false;
            }

            return ForceUnbuckle();
        }

        public bool ForceUnbuckle()
        {
            if (_buckledTo == null)
            {
                return false;
            }

            if (_buckledTo.TryGetComponent(out StrapComponent strap))
            {
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(strap.UnbuckleSound, Owner, AudioParams.Default.WithVolume(-2f));
            }

            Owner.Transform.DetachParent();
            Owner.Transform.WorldRotation = _buckledTo.Transform.WorldRotation;
            BuckledTo = null;

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(BuckleVisuals.Buckled, false);
            }

            StandingStateHelper.Standing(Owner);

            if (Owner.TryGetComponent(out SpeciesComponent species))
            {
                species.CurrentDamageState.EnterState(Owner);
            }

            BuckleStatus();

            return true;
        }

        public bool ToggleBuckle(IEntity user, IEntity to)
        {
            if (_buckledTo == null)
            {
                return TryBuckle(user, to);
            }
            else
            {
                return TryUnbuckle(user);
            }
        }

        protected override void Startup()
        {
            base.Startup();
            BuckleStatus();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            if (BuckledTo != null && BuckledTo.TryGetComponent(out StrapComponent strap))
            {
                strap.RemoveEntity(Owner);
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle(eventArgs.User);
        }

        bool IActionBlocker.CanMove()
        {
            return BuckledTo == null;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return BuckledTo == null;
        }

        [Verb]
        private sealed class BuckleVerb : Verb<BuckleComponent>
        {
            protected override void GetData(IEntity user, BuckleComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.BuckledTo == null)
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
