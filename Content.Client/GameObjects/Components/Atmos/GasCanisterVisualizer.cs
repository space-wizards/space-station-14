using Content.Shared.GameObjects.Components.Atmos;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos
{
    public class GasCanisterVisualizer : AppearanceVisualizer
    {
        [DataField("stateConnected")]
        private string? _stateConnected;

        [DataField("pressureStates")]
        private string[] _statePressure = new string[] {"", "", "", ""};

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            if (_stateConnected != null)
            {
                sprite.LayerMapSet(Layers.ConnectedToPort, sprite.AddLayerState(_stateConnected));
                sprite.LayerSetVisible(Layers.ConnectedToPort, false);

                sprite.LayerMapSet(Layers.PressureLight, sprite.AddLayerState(_stateConnected));
                sprite.LayerSetShader(Layers.PressureLight, "unshaded");
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            // Update the visuals : Is the canister connected to a port or not
            if (component.TryGetData(GasCanisterVisuals.ConnectedState, out bool isConnected))
            {
                sprite.LayerSetVisible(Layers.ConnectedToPort, isConnected);
            }

            // Update the visuals : Canister lights
            if (component.TryGetData(GasCanisterVisuals.PressureState, out int pressureState))
                if ((pressureState >= 0) && (pressureState < _statePressure.Length))
                    sprite.LayerSetState(Layers.PressureLight, _statePressure[pressureState]);
        }

        enum Layers
        {
            ConnectedToPort,
            PressureLight
        }
    }
}
