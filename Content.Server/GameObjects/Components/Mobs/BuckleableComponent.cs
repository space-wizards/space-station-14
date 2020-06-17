using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Strap;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleableComponent : Component, IActionBlocker, IInteractHand
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Buckleable";

        private IEntity _buckledTo;

        [ViewVariables] public IEntity BuckledTo => _buckledTo;

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

            // Find the first entity with a strap component to buckle the owner to
            var intersecting = _entityManager.GetEntitiesIntersecting(Owner, true);
            foreach (var intersect in intersecting)
            {
                if (!intersect.TryGetComponent(out StrapComponent strap))
                {
                    continue;
                }

                _buckledTo = intersect;
                Owner.Transform.GridPosition = intersect.Transform.GridPosition;
                Owner.Transform.AttachParent(intersect.Transform);

                switch (strap.Position)
                {
                    case StrapPosition.Stand:
                        StandingStateHelper.Standing(Owner);
                        break;
                    case StrapPosition.Down:
                        StandingStateHelper.Down(Owner);
                        break;
                }

                return true;
            }

            _notifyManager.PopupMessage(Owner, user,
                Loc.GetString("You can't buckle {0:them} there!", Owner));
            return false;
        }

        private bool TryUnbuckle()
        {
            if (_buckledTo == null)
            {
                return false;
            }

            _buckledTo = null;
            Owner.Transform.DetachParent();
            StandingStateHelper.Standing(Owner);

            if (Owner.TryGetComponent(out SpeciesComponent species))
            {
                species.CurrentDamageState.EnterState(Owner);
            }

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

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUnbuckle();
        }

        bool IActionBlocker.CanMove()
        {
            return BuckledTo == null;
        }

        [Verb]
        private sealed class BuckleVerb : Verb<BuckleableComponent>
        {
            protected override void GetData(IEntity user, BuckleableComponent component, VerbData data)
            {
                data.Text = component.BuckledTo == null ? Loc.GetString("Buckle") : Loc.GetString("Unbuckle");
            }

            protected override void Activate(IEntity user, BuckleableComponent component)
            {
                component.ToggleBuckle(user);
            }
        }
    }
}
