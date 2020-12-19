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
        private bool isPlaceable;
        private bool placeCentered;
        private Vector2 positionOffset;

        public override bool IsPlaceable
        {
            get => isPlaceable;
            set
            {
                if (isPlaceable == value)
                {
                    return;
                }

                isPlaceable = value;

                Dirty();
            }
        }

        public override bool PlaceCentered
        {
            get => placeCentered;
            set
            {
                if (placeCentered == value)
                {
                    return;
                }

                placeCentered = value;

            }
        }

        public override Vector2 PositionOffset
        {
            get => positionOffset;
            set
            {
                if (positionOffset.EqualsApprox(value))
                {
                    return;
                }

                positionOffset = value;

            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not PlaceableSurfaceComponentState state)
            {
                return;
            }

            isPlaceable = state.IsPlaceable;
            placeCentered = state.PlaceCentered;
            positionOffset = state.PositionOffset;
        }
    }
}
