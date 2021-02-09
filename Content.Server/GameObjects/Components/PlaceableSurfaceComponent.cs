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
        private bool _isPlaceable;
        private bool _placeCentered;
        private Vector2 _positionOffset;

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isPlaceable, "IsPlaceable", true);
            serializer.DataField(ref _placeCentered, "placeCentered", false);
            serializer.DataField(ref _positionOffset, "positionOffset", Vector2.Zero);
        }

        public override ComponentState GetComponentState()
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
