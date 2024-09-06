using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Effects;

public sealed class ClientInnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<InnerBodyAnomalyComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnAfterHandleState(Entity<InnerBodyAnomalyComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.FallbackSprite is null)
            return;

        if (!sprite.LayerMapTryGet(ent.Comp.LayerMap, out var index))
            index = sprite.LayerMapReserveBlank(ent.Comp.LayerMap);

        if (TryComp<BodyComponent>(ent, out var body) &&
            body.Prototype is not null &&
            ent.Comp.SpeciesSprites.TryGetValue(body.Prototype.Value, out var speciesSprite))
        {
            sprite.LayerSetSprite(index, speciesSprite);
        }
        else
        {
            sprite.LayerSetSprite(index, ent.Comp.FallbackSprite);
        }

        sprite.LayerSetVisible(index, true);
        sprite.LayerSetShader(index, "unshaded");
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = sprite.LayerMapGet(ent.Comp.LayerMap);
        sprite.LayerSetVisible(index, false);
    }
}
