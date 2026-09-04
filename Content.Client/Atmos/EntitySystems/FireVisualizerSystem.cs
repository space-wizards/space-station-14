using Content.Client.Atmos.Components;
using Content.Client.DisplacementMap;
using Content.Shared.Atmos;
using Content.Shared.DrawDepth;
using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed partial class FireVisualizerSystem : VisualizerSystem<FireVisualsComponent>
{
    [Dependency] private PointLightSystem _lights = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FireVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, FireVisualsComponent component, ComponentShutdown args)
    {
        if (component.LightEntity != null)
        {
            Del(component.LightEntity.Value);
            component.LightEntity = null;
        }

        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var layer, false))
        {
            SpriteSystem.RemoveLayer((uid, sprite), layer);
        }
    }

    private void OnComponentInit(EntityUid uid, FireVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), FireVisualLayers.Fire);
        SpriteSystem.LayerSetVisible((uid, sprite), FireVisualLayers.Fire, false);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
        if (component.Sprite != null)
            SpriteSystem.LayerSetRsi((uid, sprite), FireVisualLayers.Fire, new ResPath(component.Sprite));

        UpdateAppearance(uid, component, sprite, appearance);
    }

    protected override void OnAppearanceChange(EntityUid uid, FireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private void UpdateAppearance(EntityUid uid, FireVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var index, false))
        {
            SpriteSystem.LayerMapReserve((uid, sprite), FireVisualLayers.Fire);
            sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
            if (component.Sprite != null)
                SpriteSystem.LayerSetRsi((uid, sprite), FireVisualLayers.Fire, new ResPath(component.Sprite));
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out index, false))
                return;
        }

        AppearanceSystem.TryGetData<bool>(uid, FireVisuals.OnFire, out var onFire, appearance);
        AppearanceSystem.TryGetData<float>(uid, FireVisuals.FireStacks, out var fireStacks, appearance);
        AppearanceSystem.TryGetData<string?>(uid, FireVisuals.FireDisplacement, out var fireDisplacement, appearance);
        SpriteSystem.LayerSetVisible((uid, sprite), index, onFire);

        if (!onFire)
        {
            if (component.LightEntity != null)
            {
                Del(component.LightEntity.Value);
                component.LightEntity = null;
            }

            if (component.OriginalDrawDepth is { } original)
            {
                SpriteSystem.SetDrawDepth((uid, sprite), original);
                component.OriginalDrawDepth = null;
            }

            return;
        }

        // Floor carpets use FloorTiles depth; lift the whole sprite while burning so the fire layer is visible.
        if (component.OriginalDrawDepth == null && sprite.DrawDepth <= (int)Content.Shared.DrawDepth.DrawDepth.FloorTiles)
        {
            component.OriginalDrawDepth = sprite.DrawDepth;
            SpriteSystem.SetDrawDepth((uid, sprite), (int)Content.Shared.DrawDepth.DrawDepth.Effects);
        }

        // IconSmooth corners can be added after our layer; keep fire on top while burning.
        index = EnsureFireLayerOnTop(uid, component, sprite);
        SpriteSystem.LayerSetVisible((uid, sprite), index, true);

        if (fireStacks > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.AlternateState);
        else
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.NormalState);

        if (component.CurrentDisplacement != fireDisplacement)
        {
            if (fireDisplacement != null && ProtoMan.Resolve<DisplacementDataPrototype>(fireDisplacement, out var displacementProto))
                _displacement.TryAddDisplacement(displacementProto.Displacement, (uid, sprite), index, FireVisualLayers.Fire, out _);
            else
                _displacement.EnsureDisplacementIsNotOnSprite((uid, sprite), FireVisualLayers.Fire);

            component.CurrentDisplacement = fireDisplacement;
        }

        component.LightEntity ??= Spawn(null, new EntityCoordinates(uid, default));
        var light = EnsureComp<PointLightComponent>(component.LightEntity.Value);

        _lights.SetColor(component.LightEntity.Value, component.LightColor, light);

        // light needs a minimum radius to be visible at all, hence the + 1.5f
        _lights.SetRadius(component.LightEntity.Value, Math.Clamp(1.5f + component.LightRadiusPerStack * fireStacks, 0f, component.MaxLightRadius), light);
        _lights.SetEnergy(component.LightEntity.Value, Math.Clamp(1 + component.LightEnergyPerStack * fireStacks, 0f, component.MaxLightEnergy), light);

        // TODO flickering animation? Or just add a noise mask to the light? But that requires an engine PR.
    }

    /// <summary>
    /// Ensures the fire overlay layer is the topmost sprite layer so IconSmooth corners cannot cover it.
    /// </summary>
    private int EnsureFireLayerOnTop(EntityUid uid, FireVisualsComponent component, SpriteComponent sprite)
    {
        var layerCount = 0;
        foreach (var _ in sprite.AllLayers)
            layerCount++;

        if (SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var index, false)
            && index == layerCount - 1)
            return index;

        if (SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out _, false))
            SpriteSystem.RemoveLayer((uid, sprite), FireVisualLayers.Fire);

        index = SpriteSystem.LayerMapReserve((uid, sprite), FireVisualLayers.Fire);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
        if (component.Sprite != null)
            SpriteSystem.LayerSetRsi((uid, sprite), FireVisualLayers.Fire, new ResPath(component.Sprite));
        return index;
    }
}

public enum FireVisualLayers : byte
{
    Fire
}
