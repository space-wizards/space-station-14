using Robust.Shared.GameObjects;

namespace Content.Client.Markers
{
    public sealed class MarkerSystem : EntitySystem
    {
        private bool _markersVisible;

        public bool MarkersVisible
        {
            get => _markersVisible;
            set
            {
                _markersVisible = value;
                UpdateMarkers();
            }
        }

        private void UpdateMarkers()
        {
            foreach (var markerComponent in EntityManager.EntityQuery<MarkerComponent>(true))
            {
                markerComponent.UpdateVisibility();
            }
        }
    }
}
