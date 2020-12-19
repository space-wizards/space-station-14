using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPlaceableSurfaceComponent))]
    public class PlaceableSurfaceComponent : SharedPlaceableSurfaceComponent, IInteractUsing
    {
        private bool isPlaceable;
        private bool placeCentered;
        private Vector2 positionOffset;

        [ViewVariables(VVAccess.ReadWrite)]
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

                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
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

                Dirty();
            }
        }

        [ViewVariables]
        int IInteractUsing.Priority => -10;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref isPlaceable, "IsPlaceable", true);
            serializer.DataField(ref placeCentered, "PlaceCentered", false);
            serializer.DataField(ref positionOffset, "PositionOffset", Vector2.Zero);
        }

        public override ComponentState GetComponentState()
        {
            return new PlaceableSurfaceComponentState(isPlaceable);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!IsPlaceable)
                return false;

            if(!eventArgs.User.TryGetComponent<HandsComponent>(out var handComponent))
            {
                return false;
            }
            handComponent.Drop(eventArgs.Using);
            if (placeCentered)
                eventArgs.Using.Transform.WorldPosition = eventArgs.Target.Transform.WorldPosition + positionOffset;
            else
                eventArgs.Using.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return true;
        }
    }
}
