using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Effects;

public abstract class ClientInnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentAdd>(OnCompAdded);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompAdded(Entity<InnerBodyAnomalyComponent> ent, ref ComponentAdd args)
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
