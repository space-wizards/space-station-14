using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos.Piping
{
    [UsedImplicitly]
    [DataDefinition]
    public class GasFilterVisualizer : AppearanceVisualizer
    {
        [DataField("filterEnabledState")] private string _filterEnabledState = "gasFilterOn";

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent<ISpriteComponent>(out var sprite)) return;

            sprite.LayerMapReserveBlank(Layer.FilterEnabled);
            var filterEnabledLayer = sprite.LayerMapGet(Layer.FilterEnabled);
            sprite.LayerSetState(filterEnabledLayer, _filterEnabledState);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(FilterVisuals.VisualState, out FilterVisualState filterVisualState)) return;

            var filterEnabledLayer = sprite.LayerMapGet(Layer.FilterEnabled);
            sprite.LayerSetVisible(filterEnabledLayer, filterVisualState.Enabled);
        }

        public enum Layer : byte
        {
            FilterEnabled,
        }
    }
}
