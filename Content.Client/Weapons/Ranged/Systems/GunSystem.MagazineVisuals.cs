using Content.Client.Weapons.Ranged.Components;
using Content.Shared.DeadSpace.Dominator;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Rounding;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private void InitializeMagazineVisuals()
    {
        SubscribeLocalEvent<MagazineVisualsComponent, ComponentInit>(OnMagazineVisualsInit);
        SubscribeLocalEvent<MagazineVisualsComponent, AppearanceChangeEvent>(OnMagazineVisualsChange);
        SubscribeLocalEvent<MagazineVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
    }

    private void OnMagazineVisualsInit(EntityUid uid, MagazineVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;

        // Fucked up logic ahead, but it was already like that when I came here... FIX THIS PLEASE.

        if (_tagSystem.HasTag(uid, "BatteryWeaponFireModesSprites"))
        {
            if (TryComp<BatteryWeaponFireModesComponent>(uid, out var batteryFireModes))
            {
                if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                {
                    sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{component.MagSteps - 1}-{batteryFireModes.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.Mag, false);
                }

                if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{component.MagSteps - 1}-{batteryFireModes.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
                }
            }
            else if (TryComp<DominatorComponent>(uid, out var dominator))
            {
                if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                {
                    sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{component.MagSteps - 1}-{dominator.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.Mag, false);
                    sprite.LayerSetState(GunVisualLayers.Base, $"base-{dominator.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.Base, false);
                }

                if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{component.MagSteps - 1}-{dominator.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
                    sprite.LayerSetState(GunVisualLayers.Base, $"base-{dominator.CurrentFireMode}");
                    sprite.LayerSetVisible(GunVisualLayers.Base, false);
                }
            }
        }
        else
        {
            if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
            {
                sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{component.MagSteps - 1}");
                sprite.LayerSetVisible(GunVisualLayers.Mag, false);
            }

            if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
            {
                sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{component.MagSteps - 1}");
                sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, false);
            }
        }
    }

    private void OnMagazineVisualsChange(EntityUid uid, MagazineVisualsComponent component, ref AppearanceChangeEvent args)
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
                capacity = component.MagSteps;
            }

            if (!args.AppearanceData.TryGetValue(AmmoVisuals.AmmoCount, out var current))
            {
                current = component.MagSteps;
            }

            var step = ContentHelpers.RoundToLevels((int) current, (int) capacity, component.MagSteps);

            if (step == 0 && !component.ZeroVisible)
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
            if (_tagSystem.HasTag(uid, "BatteryWeaponFireModesSprites"))
            {
                if (TryComp<BatteryWeaponFireModesComponent>(uid, out var batteryFireModes))
                {
                    if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                    {
                        sprite.LayerSetVisible(GunVisualLayers.Mag, true);
                        sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{step}-{batteryFireModes.CurrentFireMode}");
                    }

                    if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                    {
                        sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, true);
                        sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{step}-{batteryFireModes.CurrentFireMode}");
                    }
                }
                else if (TryComp<DominatorComponent>(uid, out var dominator))
                {
                    if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                    {
                        sprite.LayerSetVisible(GunVisualLayers.Mag, true);
                        sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{step}-{dominator.CurrentFireMode}");
                        sprite.LayerSetVisible(GunVisualLayers.Base, true);
                        sprite.LayerSetState(GunVisualLayers.Base, $"base-{dominator.CurrentFireMode}");
                    }

                    if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                    {
                        sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, true);
                        sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{step}-{dominator.CurrentFireMode}");
                        sprite.LayerSetVisible(GunVisualLayers.Base, true);
                        sprite.LayerSetState(GunVisualLayers.Base, $"base-{dominator.CurrentFireMode}");
                    }
                }
            }
            else
            {
                if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out _))
                {
                    sprite.LayerSetVisible(GunVisualLayers.Mag, true);
                    sprite.LayerSetState(GunVisualLayers.Mag, $"{component.MagState}-{step}");
                }

                if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out _))
                {
                    sprite.LayerSetVisible(GunVisualLayers.MagUnshaded, true);
                    sprite.LayerSetState(GunVisualLayers.MagUnshaded, $"{component.MagState}-unshaded-{step}");
                }
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

    private void OnGetHeldVisuals(EntityUid uid, MagazineVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? _))
            return;

        if (!TryComp(uid, out ItemComponent? _))
            return;

        if (!TryComp(uid, out DominatorComponent? dominator))
            return;

        // I'm not sure if this is the right implementation, so if you have any ideas then... FIX THIS PLEASE.

        var layer = new PrototypeLayerData();
        var key = $"inhand-{args.Location.ToString().ToLowerInvariant()}-{dominator.CurrentFireMode}";
        layer.State = key;
        args.Layers.Add((key, layer));
    }
}
