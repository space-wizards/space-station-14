using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos.Piping
{
    [UsedImplicitly]
    [DataDefinition]
    public class PumpVisualizer : AppearanceVisualizer
    {
        [DataField("pumpEnabledState")] private string _pumpEnabledState = "pumpPressureOn";

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;

            sprite.LayerMapReserveBlank(Layer.PumpEnabled);
            var pumpEnabledLayer = sprite.LayerMapGet(Layer.PumpEnabled);
            sprite.LayerSetState(pumpEnabledLayer, _pumpEnabledState);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite)) return;
            if (!component.TryGetData(PumpVisuals.VisualState, out PumpVisualState pumpVisualState)) return;

            var pumpEnabledLayer = sprite.LayerMapGet(Layer.PumpEnabled);
            sprite.LayerSetVisible(pumpEnabledLayer, pumpVisualState.PumpEnabled);
        }

        public enum Layer : byte
        {
            PumpEnabled,
        }
    }
}
