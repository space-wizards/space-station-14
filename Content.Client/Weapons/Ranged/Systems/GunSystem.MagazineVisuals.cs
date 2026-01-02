using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Rounding;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void InitializeMagazineVisuals()
    {
        SubscribeLocalEvent<MagazineVisualsComponent, ComponentInit>(OnMagazineVisualsInit);
        SubscribeLocalEvent<MagazineVisualsComponent, AppearanceChangeEvent>(OnMagazineVisualsChange);
    }

    private void OnMagazineVisualsInit(Entity<MagazineVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite)) return;

        if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.Mag, out _, false))
        {
            _sprite.LayerSetRsiState((ent, sprite), GunVisualLayers.Mag, $"{ent.Comp.MagState}-{ent.Comp.MagSteps - 1}");
            _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.Mag, false);
        }

        if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.MagUnshaded, out _, false))
        {
            _sprite.LayerSetRsiState((ent, sprite), GunVisualLayers.MagUnshaded, $"{ent.Comp.MagState}-unshaded-{ent.Comp.MagSteps - 1}");
            _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.MagUnshaded, false);
        }
    }

    private void OnMagazineVisualsChange(Entity<MagazineVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        // tl;dr
        // 1.If no mag then hide it OR
        // 2. If step 0 isn't visible then hide it (mag or unshaded)
        // 3. Otherwise just do mag / unshaded as is
        var sprite = args.Sprite;

        if (sprite == null) return;

        if (!args.AppearanceData.TryGetValue(AmmoVisuals.MagLoaded, out var magloaded) ||
            magloaded is true)
        {
            if (!args.AppearanceData.TryGetValue(AmmoVisuals.AmmoMax, out var capacity))
            {
                capacity = ent.Comp.MagSteps;
            }

            if (!args.AppearanceData.TryGetValue(AmmoVisuals.AmmoCount, out var current))
            {
                current = ent.Comp.MagSteps;
            }

            var step = ContentHelpers.RoundToLevels((int)current, (int)capacity, ent.Comp.MagSteps);

            if (step == 0 && !ent.Comp.ZeroVisible)
            {
                if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.Mag, out _, false))
                {
                    _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.Mag, false);
                }

                if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.MagUnshaded, out _, false))
                {
                    _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.MagUnshaded, false);
                }

                return;
            }

            if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.Mag, out _, false))
            {
                _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.Mag, true);
                _sprite.LayerSetRsiState((ent, sprite), GunVisualLayers.Mag, $"{ent.Comp.MagState}-{step}");
            }

            if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.MagUnshaded, out _, false))
            {
                _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.MagUnshaded, true);
                _sprite.LayerSetRsiState((ent, sprite), GunVisualLayers.MagUnshaded, $"{ent.Comp.MagState}-unshaded-{step}");
            }
        }
        else
        {
            if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.Mag, out _, false))
            {
                _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.Mag, false);
            }

            if (_sprite.LayerMapTryGet((ent, sprite), GunVisualLayers.MagUnshaded, out _, false))
            {
                _sprite.LayerSetVisible((ent, sprite), GunVisualLayers.MagUnshaded, false);
            }
        }
    }
}
