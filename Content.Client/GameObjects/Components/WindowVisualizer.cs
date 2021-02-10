#nullable enable
using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public sealed class WindowVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.Owner.TryGetComponent(out SnapGridComponent? snapGrid))
                return;

            var lowWall = FindLowWall(snapGrid);
            if (lowWall == null)
                return;

            if (component.TryGetData(WindowVisuals.Damage, out float fraction))
            {
                var level = Math.Min(ContentHelpers.RoundToLevels(fraction, 1, 7), 5);
                if (level == 0)
                {
                    foreach (var val in Enum.GetValues(typeof(WindowDamageLayers)))
                    {
                        if (val == null) continue;
                        sprite.LayerSetVisible((WindowDamageLayers) val, false);
                    }
                    return;
                }
                foreach (var val in Enum.GetValues(typeof(WindowDamageLayers)))
                {
                    if (val == null) continue;
                    sprite.LayerSetVisible((WindowDamageLayers) val, true);
                }

                sprite.LayerSetState(WindowDamageLayers.DamageNE, $"{(int) lowWall.LastCornerNE}_{level}");
                sprite.LayerSetState(WindowDamageLayers.DamageSE, $"{(int) lowWall.LastCornerSE}_{level}");
                sprite.LayerSetState(WindowDamageLayers.DamageSW, $"{(int) lowWall.LastCornerSW}_{level}");
                sprite.LayerSetState(WindowDamageLayers.DamageNW, $"{(int) lowWall.LastCornerNW}_{level}");

            }
        }

        private static LowWallComponent? FindLowWall(SnapGridComponent snapGrid)
        {
            foreach (var entity in snapGrid.GetLocal())
            {
                if (entity.TryGetComponent(out LowWallComponent? lowWall))
                {
                    return lowWall;
                }
            }
            return null;
        }
    }
}
