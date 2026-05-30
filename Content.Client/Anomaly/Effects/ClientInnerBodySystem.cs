using Content.Client.DisplacementMap;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Anomaly.Effects;

public sealed partial class ClientInnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    [Dependency] private EntityQuery<InnerBodyAnomalyVisualsComponent> _visualsQuery = default!;

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

        if (TryComp<HumanoidProfileComponent>(ent, out var humanoid) &&
            ent.Comp.SpeciesSprites.TryGetValue(humanoid.Species, out var speciesSprite))
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, speciesSprite);
        }
        else
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, ent.Comp.FallbackSprite);
        }

        _sprite.LayerSetVisible((ent.Owner, sprite), index, true);
        sprite.LayerSetShader(index, "unshaded");

        if (_visualsQuery.TryGetComponent(ent, out var visuals) && visuals.Displacement != null)
        {
            if (_prototype.Resolve(visuals.Displacement, out var displacement))
            {
                _displacement.TryAddDisplacement(displacement.Displacement,
                    (ent.Owner, sprite),
                    index,
                    ent.Comp.LayerMap,
                    out _);
            }
            else
            {
                _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), ent.Comp.LayerMap);
            }
        }
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = _sprite.LayerMapGet((ent.Owner, sprite), ent.Comp.LayerMap);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, false);

        _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), ent.Comp.LayerMap);
    }
}
