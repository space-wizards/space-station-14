using Content.Shared.GameObjects.Components.Portal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [UsedImplicitly]
    public class PortalVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.Portal, sprite.AddLayerState("portal-pending"));
            sprite.LayerSetShader(Layers.Portal, "unshaded");

        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<PortalState>(PortalVisuals.State, out var state))
            {
                switch (state)
                {
                    case PortalState.Pending:
                        sprite.LayerSetState(Layers.Portal, "portal-pending");
                        break;
                    // TODO: Spritework here?
                    case PortalState.UnableToTeleport:
                        sprite.LayerSetState(Layers.Portal, "portal-unconnected");
                        break;
                    case PortalState.RecentlyTeleported:
                        sprite.LayerSetState(Layers.Portal, "portal-unconnected");
                        break;
                }
            }
            else
            {
                sprite.LayerSetState(Layers.Portal, "portal-pending");
            }
        }

        enum Layers : byte
        {
            Portal
        }
    }
}
