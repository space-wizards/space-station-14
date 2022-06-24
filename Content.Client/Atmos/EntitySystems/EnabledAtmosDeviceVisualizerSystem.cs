using Content.Client.Atmos.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public abstract class EnabledAtmosDeviceVisualizerSystem<T> : VisualizerSystem<T>
    where T: EnabledAtmosDeviceComponent
{
    protected abstract object LayerMap { get; }
    protected abstract Enum DataKey { get;  }

    protected override void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && args.Component.TryGetData(DataKey, out bool enabled)
            && sprite.LayerMapTryGet(LayerMap, out var layer))
        {
            sprite.LayerSetState(layer, enabled ? component.EnabledState : component.DisabledState);
        }
    }
}
