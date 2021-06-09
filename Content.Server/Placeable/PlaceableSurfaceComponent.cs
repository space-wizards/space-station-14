using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Placeable
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPlaceableSurfaceComponent))]
    public class PlaceableSurfaceComponent : SharedPlaceableSurfaceComponent, IInteractUsing
    {
        [DataField("placeCentered")]
        private bool _placeCentered;

        [DataField("positionOffset")]
        private Vector2 _positionOffset;

        [DataField("IsPlaceable")]
        private bool _isPlaceable = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public override bool IsPlaceable
        {
            get => _isPlaceable;
            set
            {
                if (_isPlaceable == value)
                {
                    return;
                }

                _isPlaceable = value;

                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public override bool PlaceCentered
        {
            get => _placeCentered;
            set
            {
                if (_placeCentered == value)
                {
                    return;
                }

                _placeCentered = value;

                Dirty();

            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public override Vector2 PositionOffset
        {
            get => _positionOffset;
            set
            {
                if (_positionOffset.EqualsApprox(value))
                {
                    return;
                }

                _positionOffset = value;

                Dirty();

            }
        }

        [ViewVariables]
        int IInteractUsing.Priority => -10;

        public override ComponentState GetComponentState(ICommonSession session)
        {
            return new PlaceableSurfaceComponentState(_isPlaceable,_placeCentered,_positionOffset);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!IsPlaceable)
                return false;

            if(!eventArgs.User.TryGetComponent<HandsComponent>(out var handComponent))
            {
                return false;
            }
            handComponent.Drop(eventArgs.Using);
            if (_placeCentered)
                eventArgs.Using.Transform.WorldPosition = eventArgs.Target.Transform.WorldPosition + _positionOffset;
            else
                eventArgs.Using.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return true;
        }
    }
}
