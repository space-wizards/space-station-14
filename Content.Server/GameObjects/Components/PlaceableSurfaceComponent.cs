using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    public class PlaceableSurfaceComponent : Component, IAttackBy
    {
        public override string Name => "PlaceableSurface";

        private bool _isPlaceable;
        public bool IsPlaceable { get => _isPlaceable; set => _isPlaceable = value; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isPlaceable, "IsPlaceable", true);
        }
        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent<HandsComponent>(out var handComponent))
            {
                return true;
            }
            handComponent.Drop(eventArgs.AttackWith);
            eventArgs.AttackWith.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return true;
        }
    }
}
