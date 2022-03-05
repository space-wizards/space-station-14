using Content.Shared.Atmos.Piping.Binary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class GasCanisterVisualizer : AppearanceVisualizer
    {
        [DataField("pressureStates")]
        private readonly string[] _statePressure = {"", "", "", ""};

        [DataField("insertedTankState")]
        private readonly string _insertedTankState = string.Empty;

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapSet(Layers.PressureLight, sprite.AddLayerState(_statePressure[0]));
            sprite.LayerSetShader(Layers.PressureLight, "unshaded");
            sprite.LayerMapSet(Layers.TankInserted, sprite.AddLayerState(_insertedTankState));
            sprite.LayerSetVisible(Layers.TankInserted, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
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
