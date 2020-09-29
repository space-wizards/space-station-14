using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class PlaceableSurfaceComponent : SharedPlaceableSurfaceComponent, IInteractUsing
    {
        private bool _isPlaceable;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsPlaceable { get => _isPlaceable; set => _isPlaceable = value; }

        [ViewVariables]
        int IInteractUsing.Priority => 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isPlaceable, "IsPlaceable", true);
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
