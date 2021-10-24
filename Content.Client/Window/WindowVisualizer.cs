using System;
using Content.Shared.Rounding;
using Content.Shared.Window;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Window
{
    [UsedImplicitly]
    public sealed class WindowVisualizer : AppearanceVisualizer
    {
        [DataField("crackRsi")]
        public ResourcePath CrackRsi { get; } = new ("/Textures/Structures/Windows/cracks.rsi");

        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.TryGetComponent(out ISpriteComponent? sprite))
                return;

            sprite.LayerMapReserveBlank(WindowDamageLayers.Layer);
            sprite.LayerSetVisible(WindowDamageLayers.Layer, false);
            sprite.LayerSetRSI(WindowDamageLayers.Layer, CrackRsi);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                return;

            if (component.TryGetData(WindowVisuals.Damage, out float fraction))
            {
                var level = Math.Min(ContentHelpers.RoundToLevels(fraction, 1, 5), 3);

                if (level == 0)
                {
                    sprite.LayerSetVisible(WindowDamageLayers.Layer, false);
                    return;
                }

                sprite.LayerSetVisible(WindowDamageLayers.Layer, true);
                sprite.LayerSetState(WindowDamageLayers.Layer, $"{level}");
            }
        }

        public enum WindowDamageLayers : byte
        {
            Layer,
        }
    }
}
