#nullable enable
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPlaceableSurfaceComponent))]
    public class PlaceableSurfaceComponent : SharedPlaceableSurfaceComponent
    {
        private bool _isPlaceable;
        private bool _placeCentered;
        private Vector2 _positionOffset;

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

            }
        }

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

            }
        }

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

            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not PlaceableSurfaceComponentState state)
            {
                return;
            }

            _isPlaceable = state.IsPlaceable;
            _placeCentered = state.PlaceCentered;
            _positionOffset = state.PositionOffset;
        }
    }
}
