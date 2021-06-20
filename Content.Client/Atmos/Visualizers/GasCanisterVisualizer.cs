using Content.Shared.Atmos.Piping.Binary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class GasCanisterVisualizer : AppearanceVisualizer
    {
        [DataField("pressureStates")]
        private readonly string[] _statePressure = {"", "", "", ""};

        [DataField("insertedTankState")]
        private readonly string _insertedTankState = string.Empty;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.PressureLight, sprite.AddLayerState(_statePressure[0]));
            sprite.LayerSetShader(Layers.PressureLight, "unshaded");
            sprite.LayerMapSet(Layers.TankInserted, sprite.AddLayerState(_insertedTankState));
            sprite.LayerSetVisible(Layers.TankInserted, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            // Update the canister lights
            if (component.TryGetData(GasCanisterVisuals.PressureState, out int pressureState))
                if ((pressureState >= 0) && (pressureState < _statePressure.Length))
                    sprite.LayerSetState(Layers.PressureLight, _statePressure[pressureState]);

            if(component.TryGetData(GasCanisterVisuals.TankInserted, out bool inserted))
                sprite.LayerSetVisible(Layers.TankInserted, inserted);
        }

        private enum Layers
        {
            PressureLight,
            TankInserted,
        }
    }
}
