using Content.Client.DisplacementMap;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Effects;

public sealed partial class ClientInnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    [Dependency] private EntityQuery<InnerBodyAnomalyVisualsComponent> _visualsQuery = default!;
    [Dependency] private EntityQuery<InnerBodyAnomalyComponent> _anomalyQuery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InnerBodyAnomalyComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<InnerBodyAnomalyVisualsComponent, AfterAutoHandleStateEvent>(OnVisualsAfterHandleState);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<InnerBodyAnomalyVisualsComponent, ComponentShutdown>(OnVisualsShutdown);
    }

    private void UpdateVisuals(Entity<InnerBodyAnomalyComponent, InnerBodyAnomalyVisualsComponent?> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp1.FallbackSprite is null)
            return;

        var index = _sprite.LayerMapReserve((ent.Owner, sprite), ent.Comp1.LayerMap);

        if (TryComp<HumanoidProfileComponent>(ent, out var humanoid) &&
            ent.Comp1.SpeciesSprites.TryGetValue(humanoid.Species, out var speciesSprite))
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, speciesSprite);
        }
        else
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, ent.Comp1.FallbackSprite);
        }

        _sprite.LayerSetVisible((ent.Owner, sprite), index, true);
        sprite.LayerSetShader(index, "unshaded");

        if (ent.Comp2 != null && ent.Comp2.Displacement != null && ProtoMan.Resolve(ent.Comp2.Displacement, out var displacement))
        {
            _displacement.TryAddDisplacement(displacement.Displacement,
                (ent.Owner, sprite),
                index,
                ent.Comp1.LayerMap,
                out _);
        }
        else
        {
            _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), ent.Comp1.LayerMap, index);
        }
    }

    private void OnAfterHandleState(Entity<InnerBodyAnomalyComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _visualsQuery.TryGetComponent(ent.Owner, out var visuals);
        UpdateVisuals((ent, ent.Comp, visuals));
    }

    private void OnVisualsAfterHandleState(Entity<InnerBodyAnomalyVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_anomalyQuery.TryGetComponent(ent.Owner, out var anomaly))
            UpdateVisuals((ent.Owner, anomaly, ent.Comp));
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = _sprite.LayerMapGet((ent.Owner, sprite), ent.Comp.LayerMap);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, false);

        _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), ent.Comp.LayerMap, index);
    }

    private void OnVisualsShutdown(Entity<InnerBodyAnomalyVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!TryComp<InnerBodyAnomalyComponent>(ent, out var anomaly))
            return;

        var index = _sprite.LayerMapGet((ent.Owner, sprite), anomaly.LayerMap);

        _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), anomaly.LayerMap, index);
    }
}
