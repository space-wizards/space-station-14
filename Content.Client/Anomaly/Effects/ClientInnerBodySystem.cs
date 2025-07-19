using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Effects;

public sealed class ClientInnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        var index = _sprite.LayerMapReserve((ent.Owner, sprite), ent.Comp.LayerMap);

        if (TryComp<BodyComponent>(ent, out var body) &&
            body.Prototype is not null &&
            ent.Comp.SpeciesSprites.TryGetValue(body.Prototype.Value, out var speciesSprite))
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, speciesSprite);
        }
        else
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, ent.Comp.FallbackSprite);
        }

        _sprite.LayerSetVisible((ent.Owner, sprite), index, true);
        sprite.LayerSetShader(index, "unshaded");
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = _sprite.LayerMapGet((ent.Owner, sprite), ent.Comp.LayerMap);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, false);
    }
}
