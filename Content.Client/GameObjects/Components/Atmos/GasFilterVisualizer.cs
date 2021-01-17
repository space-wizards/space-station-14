#nullable enable
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class GasFilterVisualizer : AppearanceVisualizer
    {
        private string _filterEnabledState = default!;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _filterEnabledState, "filterEnabledState", "gasFilterOn");
        }

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
