using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent
    {
        private bool _buckled;
        private int? _originalDrawDepth;

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

        protected override bool Buckled => _buckled;
    }
}
