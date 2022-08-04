using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Client.GameObjects;

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
    }

    private void OnComponentInit(EntityUid uid, FireVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(FireVisualLayers.Fire);
        sprite.LayerSetVisible(FireVisualLayers.Fire, false);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
    }

    protected override void OnAppearanceChange(EntityUid uid, FireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!args.Component.TryGetData(FireVisuals.OnFire, out bool onFire) ||
            !TryComp<SpriteComponent>(component.Owner, out var sprite))
            return;

        var fireStacks = 0f;
        if (args.Component.TryGetData(FireVisuals.FireStacks, out float stacks))
            fireStacks = stacks;

        SetOnFire(sprite, args.Component, component, onFire, fireStacks);
    }

    private void SetOnFire(SpriteComponent sprite, AppearanceComponent appearance, FireVisualsComponent component, bool onFire, float fireStacks)
    {
        if (component.Sprite != null)
            sprite.LayerSetRSI(FireVisualLayers.Fire, component.Sprite);

        sprite.LayerSetVisible(FireVisualLayers.Fire, onFire);

        if(fireStacks > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
            sprite.LayerSetState(FireVisualLayers.Fire, component.AlternateState);
        else
            sprite.LayerSetState(FireVisualLayers.Fire, component.NormalState);
    }
}

public enum FireVisualLayers : byte
{
    Fire
}
