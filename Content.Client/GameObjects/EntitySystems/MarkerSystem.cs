using Content.Client.GameObjects.Components.Markers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class MarkerSystem : EntitySystem
    {
        private bool _markersVisible;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<MarkerComponent>();
        }

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
            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<MarkerComponent>().UpdateVisibility();
            }
        }
    }
}
