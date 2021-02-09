#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.Damage
{
    [UsedImplicitly]
    public class DamageVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(DamageVisualizerData.Layers, out Dictionary<int, ThresholdAppearance> layers))
            {
                return;
            }

            foreach (var appearance in layers.Values)
            {
                if (appearance.State == null)
                {
                    return;
                }

                if (appearance.Sprite != null)
                {
                    var path = SharedSpriteComponent.TextureRoot / appearance.Sprite;
                    var rsi = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(path).RSI;

                    if (appearance.Layer == null)
                    {
                        sprite.BaseRSI = rsi;
                    }
                    else
                    {
                        sprite.LayerSetRSI(appearance.Layer.Value, rsi);
                    }
                }

                var layerKey = appearance.Layer ?? 0;

                sprite.LayerMapReserveBlank(layerKey);
                sprite.LayerSetState(layerKey, appearance.State);
            }
        }
    }
}
