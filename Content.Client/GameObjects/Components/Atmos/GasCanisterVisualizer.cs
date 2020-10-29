using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFluidsynth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    public class GasCanisterVisualizer : AppearanceVisualizer
    {
        private string _sprite;
        private string _stateConnected;

        public bool ConnectedToPort = false;

        private enum VisualLayers
        {
            ConnectedToPort = 1,
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _sprite = node.GetNode("sprite").AsString();
            _stateConnected = node.GetNode("stateConnected").AsString();
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var appearance = entity.EnsureComponent<AppearanceComponent>();

            if (appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {

                // Add new layers
                sprite.AddLayer(
                    new SpriteSpecifier.Rsi(new ResourcePath(_sprite), _stateConnected),
                    (int) VisualLayers.ConnectedToPort);

                sprite.LayerSetVisible((int) VisualLayers.ConnectedToPort, false);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            // Update the visuals : Is the canister connected to a port or not
            if (component.TryGetData(GasCanisterVisuals.ConnectedState, out bool isConnected))
            {
                sprite.LayerSetVisible((int) VisualLayers.ConnectedToPort, isConnected);
            }

        }
    }
}
