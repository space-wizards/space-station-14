using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public abstract class EnabledAtmosDeviceVisualizer : AppearanceVisualizer
    {
        [DataField("disabledState")]
        private string _disabledState = string.Empty;
        [DataField("enabledState")]
        private string _enabledState = string.Empty;
        protected abstract object LayerMap { get; }
        protected abstract Enum DataKey { get; }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                return;

            if(component.TryGetData(DataKey, out bool enabled) && sprite.LayerMapTryGet(LayerMap, out var layer))
                sprite.LayerSetState(layer, enabled ? _enabledState : _disabledState);
        }
    }
}
