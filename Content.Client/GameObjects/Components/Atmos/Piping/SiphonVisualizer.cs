using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos.Piping
{
    [UsedImplicitly]
    [DataDefinition]
    public class SiphonVisualizer : AppearanceVisualizer
    {
        [DataField("siphonOnState")] private string _siphonOnState = "scrubOn";

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;

            sprite.LayerMapReserveBlank(Layer.SiphonEnabled);
            var layer = sprite.LayerMapGet(Layer.SiphonEnabled);
            sprite.LayerSetState(layer, _siphonOnState);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite)) return;
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
