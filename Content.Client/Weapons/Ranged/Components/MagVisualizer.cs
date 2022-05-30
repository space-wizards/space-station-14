using Content.Shared.Rounding;
using Content.Shared.Weapons.Ranged;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Components;

[UsedImplicitly]
public sealed class MagVisualizer : AppearanceVisualizer
{
    private bool _magLoaded;
    [DataField("magState")] private string? _magState;
    [DataField("steps")] private int _magSteps;
    [DataField("zeroVisible")] private bool _zeroVisible;

    public override void InitializeEntity(EntityUid entity)
    {
        base.InitializeEntity(entity);
        var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

        if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
        {
            sprite.LayerSetState(GunVisualLayers.Mag, $"{_magState}-{_magSteps - 1}");
            sprite.LayerSetVisible(GunVisualLayers.Mag, false);
        }

        if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
        {
            sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{_magState}-unshaded-{_magSteps - 1}");
            sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
        }
    }

    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);

        // tl;dr
        // 1.If no mag then hide it OR
        // 2. If step 0 isn't visible then hide it (mag or unshaded)
        // 3. Otherwise just do mag / unshaded as is
        var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

        component.TryGetData(SharedGunSystem.AmmoVisuals.MagLoaded, out _magLoaded);

        if (_magLoaded)
        {
            if (!component.TryGetData(SharedGunSystem.AmmoVisuals.AmmoMax, out int capacity))
            {
                return;
            }

            if (!component.TryGetData(SharedGunSystem.AmmoVisuals.AmmoCount, out int current))
            {
                return;
            }

            var step = ContentHelpers.RoundToLevels(current, capacity, _magSteps);

            if (step == 0 && !_zeroVisible)
            {
                if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                {
                    sprite.LayerSetVisible(GunVisualLayers.Mag, false);
                }

                if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
                }

                return;
            }

            if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
            {
                sprite.LayerSetVisible(GunVisualLayers.Mag, true);
                sprite.LayerSetState(GunVisualLayers.Mag, $"{_magState}-{step}");
            }

            if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, true);
                sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{_magState}-unshaded-{step}");
            }
        }
        else
        {
            if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
            {
                sprite.LayerSetVisible(GunVisualLayers.Mag, false);
            }

            if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
            }
        }
    }
}

public enum GunVisualLayers
{
    Base,
    Mag,
    MagUnshaded,
}
