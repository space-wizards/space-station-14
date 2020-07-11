using Content.Client.GameObjects.Components.Strap;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Buckle;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Buckle
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent, IClientDraggable
    {
        private bool _buckled;
        private int? _originalDrawDepth;

        public override bool Buckled => _buckled;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is BuckleComponentState buckle))
            {
                return;
            }

            _buckled = buckle.Buckled;

            if (!Owner.TryGetComponent(out SpriteComponent ownerSprite))
            {
                return;
            }

            if (_buckled && buckle.DrawDepth.HasValue)
            {
                _originalDrawDepth ??= ownerSprite.DrawDepth;
                ownerSprite.DrawDepth = buckle.DrawDepth.Value;
                return;
            }

            if (!_buckled && _originalDrawDepth.HasValue)
            {
                ownerSprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }

        bool IClientDraggable.ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<StrapComponent>();
        }

        bool IClientDraggable.ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
