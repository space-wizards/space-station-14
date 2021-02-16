using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos.Piping
{
    [UsedImplicitly]
    public class SiphonVisualizer : AppearanceVisualizer
    {
        private string? _siphonOnState;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _siphonOnState, "siphonOnState", "scrubOn");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent? sprite)) return;

            sprite.LayerMapReserveBlank(Layer.SiphonEnabled);
            var layer = sprite.LayerMapGet(Layer.SiphonEnabled);
            sprite.LayerSetState(layer, _siphonOnState);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;
            if (!component.TryGetData(SiphonVisuals.VisualState, out SiphonVisualState siphonVisualState)) return;

            var layer = sprite.LayerMapGet(Layer.SiphonEnabled);
            sprite.LayerSetVisible(layer, siphonVisualState.SiphonEnabled);
        }

        private enum Layer : byte
        {
            SiphonEnabled,
        }
    }
}
