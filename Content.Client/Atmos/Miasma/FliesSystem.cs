using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.Miasma;

public sealed class FliesSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FliesComponent, ComponentStartup>(FliesAdded);
        SubscribeLocalEvent<FliesComponent, ComponentShutdown>(FliesRemoved);
    }

    private void FliesRemoved(EntityUid uid, FliesComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(FliesKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private void FliesAdded(EntityUid uid, FliesComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(FliesKey.Key, out var _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/flies.rsi"), "flies"));
        sprite.LayerMapSet(FliesKey.Key, layer);
    }

    private enum FliesKey
    {
        Key,
    }
}
