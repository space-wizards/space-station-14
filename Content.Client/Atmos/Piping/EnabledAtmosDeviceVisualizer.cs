using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Piping
{
    [UsedImplicitly]
    public abstract class EnabledAtmosDeviceVisualizer : AppearanceVisualizer
    {
        [DataField("enabledState")]
        private string _enabledState = string.Empty;
        protected abstract object LayerMap { get; }
        protected abstract Enum DataKey { get; }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent? sprite))
                return;

            sprite.LayerMapSet(LayerMap, sprite.AddLayerState(_enabledState));
            sprite.LayerSetVisible(LayerMap, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                return;

            if(component.TryGetData(DataKey, out bool enabled))
                sprite.LayerSetVisible(LayerMap, enabled);
        }
    }
}
