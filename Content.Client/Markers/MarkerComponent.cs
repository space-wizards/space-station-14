using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Markers
{
    [RegisterComponent]
    public sealed class MarkerComponent : Component
    {
        public override string Name => "Marker";

        protected override void Startup()
        {
            base.Startup();

            UpdateVisibility();
        }

        public void UpdateVisibility()
        {
            var system = EntitySystem.Get<MarkerSystem>();

            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.Visible = system.MarkersVisible;
            }
        }
    }
}
