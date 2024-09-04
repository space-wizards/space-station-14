using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Effects;

public sealed class ClientInnerBodyAnomalySystem : EntitySystem
{
    public override void Initialize()
    {
        //SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentStartup>(OnCompStartup);
        //SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompStartup(Entity<InnerBodyAnomalyComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.LayerSprite is null)
            return;

        var index = sprite.LayerMapGet(ent.Comp.LayerMap);
        sprite.LayerSetSprite(index, ent.Comp.LayerSprite);
        sprite.LayerSetVisible(index, true);
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = sprite.LayerMapGet(ent.Comp.LayerMap);
        sprite.LayerSetVisible(index, false);
    }
}
