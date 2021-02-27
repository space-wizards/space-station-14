#nullable enable
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class SolutionContainerVisualizer : AppearanceVisualizer
    {
        private int _maxFillLevels;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _maxFillLevels, "maxFillLevels", 0);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (_maxFillLevels <= 0) return;

            if (!component.TryGetData(SolutionContainerVisuals.VisualState,
                out SolutionContainerVisualState state)) return;

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;
            if (!sprite.LayerMapTryGet(SolutionContainerLayers.Fill, out var fillLayer)) return;

            var fillPercent = state.FilledVolumePercent;
            var closestFillSprite = (int) (fillPercent * _maxFillLevels);

            //sprite.LayerSetState(fillLayer)
        }
    }
}
