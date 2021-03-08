#nullable enable
using Content.Shared.GameObjects.Components.Buckle;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Buckle
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public class BuckleComponent : SharedBuckleComponent
    {
        private bool _buckled;
        private int? _originalDrawDepth;

        public override bool Buckled => _buckled;

        public override bool TryBuckle(IEntity? user, IEntity to)
        {
            // TODO: Prediction
            return false;
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not BuckleComponentState buckle)
            {
                return;
            }

            _buckled = buckle.Buckled;
            LastEntityBuckledTo = buckle.LastEntityBuckledTo;
            DontCollide = buckle.DontCollide;

            if (!Owner.TryGetComponent(out SpriteComponent? ownerSprite))
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
    }
}
