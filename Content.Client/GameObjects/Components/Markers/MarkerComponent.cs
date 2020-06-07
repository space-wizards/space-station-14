using Content.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.Components.Markers
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

            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.Visible = system.MarkersVisible;
            }
        }
    }
}
