using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public sealed class WindowVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.Owner.Transform.Anchored)
                return;

            var lowWall = FindLowWall(IoCManager.Resolve<IMapManager>(), component.Owner.Transform);
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

        private static LowWallComponent? FindLowWall(IMapManager mapManager, ITransformComponent transform)
        {
            var grid = mapManager.GetGrid(transform.GridID);
            var coords = transform.Coordinates;
            foreach (var entity in grid.GetLocal(coords))
            {
                if (transform.Owner.EntityManager.ComponentManager.TryGetComponent(entity, out LowWallComponent? lowWall))
                {
                    return lowWall;
                }
            }
            return null;
        }
    }
}
