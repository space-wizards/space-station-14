using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.Components.Atmos;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class VentVisualizer : AppearanceVisualizer
    {
        private string _ventOnstate;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _ventOnstate, "ventOnState", "ventOn");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;

            sprite.LayerMapReserveBlank(Layer.VentEnabled);
            var layer = sprite.LayerMapGet(Layer.VentEnabled);
            sprite.LayerSetState(layer, _ventOnstate);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite)) return;
            if (!component.TryGetData(VentVisuals.VisualState, out VentVisualState ventVisualState)) return;

            var layer = sprite.LayerMapGet(Layer.VentEnabled);
            sprite.LayerSetVisible(layer, ventVisualState.VentEnabled);
        }

        private enum Layer : byte
        {
            VentEnabled,
        }
    }
}
