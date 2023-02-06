using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using OpenToolkit.Graphics.OpenGL;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class FireVisualizerSystem : VisualizerSystem<FireVisualsComponent>
{
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

        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.RemoveLayer(FireVisualLayers.Fire);
    }

    private void OnComponentInit(EntityUid uid, FireVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(FireVisualLayers.Fire);
        sprite.LayerSetVisible(FireVisualLayers.Fire, false);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
        if (component.Sprite != null)
            sprite.LayerSetRSI(FireVisualLayers.Fire, component.Sprite);

        UpdateAppearance(uid, component, sprite, appearance);
    }

    protected override void OnAppearanceChange(EntityUid uid, FireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private void UpdateAppearance(EntityUid uid, FireVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!sprite.LayerMapTryGet(FireVisualLayers.Fire, out var index))
            return;

        AppearanceSystem.TryGetData<bool>(uid, FireVisuals.OnFire, out var onFire, appearance);
        AppearanceSystem.TryGetData<float>(uid, FireVisuals.FireStacks, out var fireStacks, appearance);
        sprite.LayerSetVisible(index, onFire);

        if (!onFire)
        {
            if (component.LightEntity != null)
            {
                Del(component.LightEntity.Value);
                component.LightEntity = null;
            }

            return;
        }

        if (fireStacks > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
            sprite.LayerSetState(index, component.AlternateState);
        else
            sprite.LayerSetState(index, component.NormalState);

        component.LightEntity ??= Spawn(null, new EntityCoordinates(uid, default));
        var light = EnsureComp<PointLightComponent>(component.LightEntity.Value);

        light.Color = component.LightColor;

        // light needs a minimum radius to be visible at all, hence the + 1.5f
        light.Radius = Math.Clamp(1.5f + component.LightRadiusPerStack * fireStacks, 0f, component.MaxLightRadius);
        light.Energy = Math.Clamp(1 + component.LightEnergyPerStack * fireStacks, 0f, component.MaxLightEnergy);

        // TODO flickering animation? Or just add a noise mask to the light? But that requires an engine PR.
    }
}

public enum FireVisualLayers : byte
{
    Fire
}
