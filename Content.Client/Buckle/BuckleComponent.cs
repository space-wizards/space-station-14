using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public sealed class BuckleComponent : SharedBuckleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        private bool _buckled;
        private int? _originalDrawDepth;

        public override bool Buckled => _buckled;

        public override bool TryBuckle(EntityUid user, EntityUid to)
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

            _sysMan.GetEntitySystem<ActionBlockerSystem>().UpdateCanMove(Owner);

            if (!_entMan.TryGetComponent(Owner, out SpriteComponent? ownerSprite))
            {
                return;
            }

            if (LastEntityBuckledTo != null && _entMan.HasComponent<VehicleComponent>(LastEntityBuckledTo))
            {
                return;
            }

            // TODO when ECSing, make this a visualizer
            if (_buckled &&
                LastEntityBuckledTo != null &&
                EntMan.GetComponent<TransformComponent>(LastEntityBuckledTo.Value).LocalRotation.GetCardinalDir() == Direction.North &&
                EntMan.TryGetComponent<SpriteComponent>(LastEntityBuckledTo, out var buckledSprite))
            {
                _originalDrawDepth ??= ownerSprite.DrawDepth;
                ownerSprite.DrawDepth = buckledSprite.DrawDepth - 1;
                return;
            }

            if (_originalDrawDepth.HasValue && !_buckled)
            {
                ownerSprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }
    }
}
