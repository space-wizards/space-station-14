using Content.Shared.GameObjects.Components.Atmos;
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
        private string[] _statePressure = new string[] {"", "", "", ""};

        private enum VisualLayers
        {
            ConnectedToPort = 1,
            PressureLight = 2
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _sprite = node.GetNode("sprite").AsString();
            _stateConnected = node.GetNode("stateConnected").AsString();
            for (int i = 0; i < _statePressure.Length; i++)
                _statePressure[i] = node.GetNode("stateO" + i).AsString();
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

                sprite.AddLayer(
                    new SpriteSpecifier.Rsi(new ResourcePath(_sprite), _statePressure[0]),
                    (int) VisualLayers.PressureLight);

                sprite.LayerSetShader((int) VisualLayers.PressureLight, "unshaded");
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

            // Update the visuals : Canister lights
            if (component.TryGetData(GasCanisterVisuals.PressureState, out int pressureState))
                if ((pressureState >= 0) && (pressureState < _statePressure.Length))
                    sprite.LayerSetState((int) VisualLayers.PressureLight, _statePressure[pressureState]);
        }
    }
}
