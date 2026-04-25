using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This system handles sprite changes for a <see cref="IGasMaxPressureHolder"/>
/// with a <see cref="MaxPressureVisualsComponent"/> when its <see cref="IGasMaxPressureHolder.Integrity"/> changes.
/// </summary>
public sealed class MaxPressureVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MaxPressureVisualsComponent, ComponentInit>(OnMaxPressureInit);
        SubscribeLocalEvent<MaxPressureVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnMaxPressureInit(Entity<MaxPressureVisualsComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Comp.IntegritySteps);

        if (_sprite.LayerMapTryGet((entity, sprite), MaxPressureVisualLayers.Base, out _, false))
        {
            _sprite.LayerSetRsiState((entity, sprite), MaxPressureVisualLayers.Base, $"{entity.Comp.IntegrityMask}");
            _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.Base, false);
        }

        if (_sprite.LayerMapTryGet((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, out _, false))
        {
            _sprite.LayerSetRsiState((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, $"{entity.Comp.IntegrityState}-unshaded-0");
            _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, false);
        }
    }

    private void OnAppearanceChange(Entity<MaxPressureVisualsComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!args.AppearanceData.TryGetValue(GasIntegrity.Integrity, out var obj) || obj is not float integrity)
            return;

        if (!args.AppearanceData.TryGetValue(GasIntegrity.MaxIntegrity, out obj) || obj is not float maxIntegrity)
            return;

        // We don't want visuals at max integrity, so we return if we're at max.
        if (integrity >= maxIntegrity)
        {
            _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.Base, false);
            _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, false);
            return;
        }

        _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.Base, true);
        _sprite.LayerSetVisible((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, true);

        // Subtract our integrity + 1 to get an accurate step count.
        if (entity.Comp.IntegritySteps > 1)
        {
            var step = ContentHelpers.RoundToEqualLevels(maxIntegrity - integrity - 1, maxIntegrity, entity.Comp.IntegritySteps);
            _sprite.LayerSetRsiState((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, $"{entity.Comp.IntegrityState}-unshaded-{step}");
        }
        else
        {
            _sprite.LayerSetRsiState((entity, sprite), MaxPressureVisualLayers.BaseUnshaded, $"{entity.Comp.IntegrityState}-unshaded-0");
        }
    }
}
