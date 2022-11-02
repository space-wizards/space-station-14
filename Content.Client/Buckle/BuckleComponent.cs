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

            // Adjust draw depth when the chair faces north so that the seat back is drawn over the player.
            // Reset the draw depth when rotated in any other direction.
            // TODO when ECSing, make this a visualizer
            // This code was written before rotatable viewports were introduced, so hard-coding Direction.North
            // and comparing it against LocalRotation now breaks this in other rotations. This is a FIXME, but
            // better to get it working for most people before we look at a more permanent solution.
            if (_buckled &&
                LastEntityBuckledTo != null &&
                EntMan.GetComponent<TransformComponent>(LastEntityBuckledTo.Value).LocalRotation.GetCardinalDir() == Direction.North &&
                EntMan.TryGetComponent<SpriteComponent>(LastEntityBuckledTo, out var buckledSprite))
            {
                _originalDrawDepth ??= ownerSprite.DrawDepth;
                ownerSprite.DrawDepth = buckledSprite.DrawDepth - 1;
                return;
            }

            // If here, we're not turning north and should restore the saved draw depth.
            if (_originalDrawDepth.HasValue)
            {
                ownerSprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }
    }
}
