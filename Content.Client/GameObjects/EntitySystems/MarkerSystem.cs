using Content.Client.GameObjects.Components.Markers;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
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
            foreach (var markerComponent in EntityManager.ComponentManager.EntityQuery<MarkerComponent>(true))
            {
                markerComponent.UpdateVisibility();
            }
        }
    }
}
