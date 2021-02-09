#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Destructible.Thresholds.Behaviors;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    public class ChangeAppearanceBehavior : IThresholdBehavior
    {
        [ViewVariables] public ThresholdAppearance Appearance { get; private set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Appearance, "appearance", default);
        }

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (!owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                return;
            }

            if (!appearanceComponent.TryGetData(DamageVisualizerData.Layers, out Dictionary<int, ThresholdAppearance>? layers))
            {
                layers = new Dictionary<int, ThresholdAppearance>();
            }

            var layerIndex = Appearance.Layer ?? 0;
            layers[layerIndex] = Appearance;

            appearanceComponent.SetData(DamageVisualizerData.Layers, layers);
            appearanceComponent.Dirty();
        }
    }
}
