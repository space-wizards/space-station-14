using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Strap;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleableComponent : Component, IActionBlocker, IInteractHand, IMoveSpeedModifier
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Buckleable";

        private IEntity _buckled;

        [ViewVariables] public IEntity Buckled => _buckled;
        public float WalkSpeedModifier => Buckled == null ? 1f : 0f;
        public float SprintSpeedModifier => Buckled == null ? 1f : 0f;

        public bool TryBuckle(IEntity buckler, IEntity to)
        {
            if (buckler == null || to == null)
            {
                return false;
            }

            if (!buckler.TryGetComponent(out HandsComponent hands))
            {
                _notifyManager.PopupMessage(buckler, buckler,
                    Loc.GetString("You don't have hands!"));
                return false;
            }

            if (hands.GetActiveHand != null)
            {
                _notifyManager.PopupMessage(buckler, buckler,
                    Loc.GetString("Your hand isn't free!"));
                return false;
            }

            if (_buckled != null)
            {
                _notifyManager.PopupMessage(Owner, buckler,
                    Loc.GetString("{0:They} are already buckled in!", Owner));
                return false;
            }

            if (!to.HasComponent<StrapComponent>())
            {
                _notifyManager.PopupMessage(Owner, buckler,
                    Loc.GetString("You can't buckle {0:them} there!", Owner));
                return false;
            }

            _buckled = to;
            return true;
        }

        public void TryUnbuckle()
        {
            _buckled = null;
        }

        public bool TryReBuckle(IEntity buckler, IEntity to)
        {
            TryUnbuckle();
            return TryBuckle(buckler, to);
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            _buckled = null;
            return true;
        }

        public bool CanMove()
        {
            return Buckled == null;
        }
    }
}
