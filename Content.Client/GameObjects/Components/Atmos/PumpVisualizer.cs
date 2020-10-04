using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PumpVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;

            sprite.LayerMapReserveBlank(Layer.PumpEnabled);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite)) return;
            if (!component.TryGetData(PumpVisuals.VisualState, out PumpVisualState pumpVisualState)) return;

            var pumpEnabledLayer = sprite.LayerMapGet(Layer.PumpEnabled);
            sprite.LayerSetVisible(pumpEnabledLayer, pumpVisualState.PumpEnabled);
        }

        public enum Layer
        {
            PumpEnabled,
        }
    }
}
