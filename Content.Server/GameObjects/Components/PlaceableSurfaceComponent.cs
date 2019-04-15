using Content.Server.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    public class PlaceableSurfaceComponent : Component
    {
        public override string Name => "PlaceableSurface";

        private bool _isPlaceable;
        public bool IsPlaceable { get => _isPlaceable; set => _isPlaceable = value; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isPlaceable, "IsPlaceable", true);
        }
    }
}
