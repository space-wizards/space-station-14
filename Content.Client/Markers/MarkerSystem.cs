using Robust.Client.GameObjects;
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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MarkerComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, MarkerComponent marker, ComponentStartup args)
        {
            UpdateVisibility(marker);
        }

        private void UpdateVisibility(MarkerComponent marker)
        {
            if (EntityManager.TryGetComponent(marker.Owner, out SpriteComponent? sprite))
            {
                sprite.Visible = MarkersVisible;
            }
        }

        private void UpdateMarkers()
        {
            foreach (var markerComponent in EntityManager.EntityQuery<MarkerComponent>(true))
            {
                UpdateVisibility(markerComponent);
            }
        }
    }
}
