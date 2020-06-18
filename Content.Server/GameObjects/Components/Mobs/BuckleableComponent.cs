using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Strap;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleableComponent : Component, IActionBlocker, IInteractHand
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystem;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Buckleable";

        private IEntity _buckledTo;

        [ViewVariables] public IEntity BuckledTo => _buckledTo;

        private IEnumerable<StrapComponent> FindStrappables()
        {
            var intersecting = _entityManager.GetEntitiesIntersecting(Owner, true);

            foreach (var intersect in intersecting)
            {
                if (intersect.TryGetComponent(out StrapComponent strap))
                {
                    yield return strap;
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

        private bool TryBuckle(IEntity user)
        {
            if (user == null)
            {
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

            // Find the closest entity with a strap component to buckle the owner to
            var straps = FindStrappables();
            (StrapComponent strap, float distance) closest = (null, float.PositiveInfinity);

            foreach (var strap in straps)
            {
                if (!strap.Owner.TryGetComponent(out ITransformComponent transform))
                {
                    continue;
                }

                var pos = transform.WorldPosition;
                var distance = (pos - Owner.Transform.WorldPosition).LengthSquared;

                if (distance > closest.distance)
                {
                    continue;
                }

                closest = (strap, distance);
            }

            if (closest.strap == null)
            {
                _notifyManager.PopupMessage(Owner, user,
                    Loc.GetString("You can't buckle {0:them} there!", Owner));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(closest.strap.BuckleSound, Owner, AudioParams.Default.WithVolume(-2f));
            _buckledTo = closest.strap.Owner;

            var ownTransform = Owner.Transform;
            var closestTransform = closest.strap.Owner.Transform;

            ownTransform.GridPosition = closestTransform.GridPosition;
            ownTransform.AttachParent(closestTransform);

            switch (closest.strap.Position)
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

        public bool TryUnbuckle()
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

            _buckledTo = null;
            Owner.Transform.DetachParent();
            StandingStateHelper.Standing(Owner);

            if (Owner.TryGetComponent(out SpeciesComponent species))
            {
                species.CurrentDamageState.EnterState(Owner);
            }

            BuckleStatus();

            return true;
        }

        public bool ToggleBuckle(IEntity user)
        {
            if (BuckledTo == null)
            {
                return TryBuckle(user);
            }
            else
            {
                return TryUnbuckle();
            }
        }

        protected override void Startup()
        {
            base.Startup();
            BuckleStatus();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle();
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
        private sealed class BuckleVerb : Verb<BuckleableComponent>
        {
            protected override void GetData(IEntity user, BuckleableComponent component, VerbData data)
            {
                if (!component.FindStrappables().Any())
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = component.BuckledTo == null ? Loc.GetString("Buckle") : Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, BuckleableComponent component)
            {
                component.ToggleBuckle(user);
            }
        }
    }
}
