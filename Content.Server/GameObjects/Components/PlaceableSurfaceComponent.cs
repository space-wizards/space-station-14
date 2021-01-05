using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPlaceableSurfaceComponent))]
    public class PlaceableSurfaceComponent : SharedPlaceableSurfaceComponent, IInteractUsing
    {
        [YamlField("IsPlaceable")]
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

        [ViewVariables]
        int IInteractUsing.Priority => -10;

        public override ComponentState GetComponentState()
        {
            return new PlaceableSurfaceComponentState(_isPlaceable);
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
            eventArgs.Using.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return true;
        }
    }
}
