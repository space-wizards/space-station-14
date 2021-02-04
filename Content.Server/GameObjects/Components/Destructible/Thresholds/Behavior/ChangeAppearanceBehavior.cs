#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Destructible;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    public class ChangeAppearanceBehavior : IThresholdBehavior
    {
        [ViewVariables] public ThresholdAppearance? Appearance;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Appearance, "appearance", null);
        }

        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            if (Appearance == null)
            {
                return;
            }

            if (!owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                return;
            }

            // TODO Remove layers == null see https://github.com/space-wizards/RobustToolbox/pull/1461
            if (!appearanceComponent.TryGetData(DamageVisualizerData.Layers, out Dictionary<int, ThresholdAppearance>? layers) ||
                layers == null)
            {
                layers = new Dictionary<int, ThresholdAppearance>();
            }

            var appearance = Appearance.Value;
            var layerIndex = appearance.Layer ?? 0;
            layers[layerIndex] = appearance;

            appearanceComponent.SetData(DamageVisualizerData.Layers, layers);
        }
    }
}
