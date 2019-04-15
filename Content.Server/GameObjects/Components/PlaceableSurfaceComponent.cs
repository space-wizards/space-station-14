using Content.Server.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    public class PlaceableSurfaceComponent : Component, IAfterAttack
    {
        public override string Name => "PlaceableSurface";

        private bool _isPlaceable;
        public bool IsPlaceable { get => _isPlaceable; set => _isPlaceable = value; } 

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isPlaceable, "IsPlaceable", true);
        }

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent<HandsComponent>(out var handComponent))
            {
                return;
            }
            if(eventArgs.Attacked != handComponent.GetActiveHand.Owner)
            {
                return;
            }
            handComponent.Drop(handComponent.ActiveIndex);
            eventArgs.User.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return;
        }
    }
}
