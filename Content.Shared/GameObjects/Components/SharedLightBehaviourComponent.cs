using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.GameObjects.Components
{
    // should this all be a part of the PointLightComponent instead? All it does is augment PointLightComponents. 

    /// <summary>
    /// A component which applies a specific behaviour to a PointLightComponent on its owner.
    /// </summary>
    public class SharedLightBehaviourComponent : Component 
    {
        public override string Name => "LightBehaviour";

        [Serializable, NetSerializable]
        public enum LightBehaviourType
        {
            PulseBrightness,    // a light fading in and out smoothly
            PulseSize,          // a light getting bigger and smaller smoothly
            RandomBrightness,   // something like a campfire flickering
            RandomSize,         // sort of campfire-esque as well
            Flicker,            // light turns on then off again quickly. think spooky streetlight.
            Toggle,             // light toggles itself on and off. 
            ColorSequence,      // light immediately changes colors using the predetermined sequence (or random if the sequence is empty)
            ColorSequenceSmooth // same as above but lerped
        }

        [Serializable, NetSerializable]
        protected struct LightBehaviourData : IExposeData
        {
            public LightBehaviourType LightBehaviourType;
            public float MinValue;
            public float MaxValue;
            public float MinDuration;
            public float MaxDuration;
            public List<Color> ColorsToCycle;

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref LightBehaviourType, "type", LightBehaviourType.Flicker);
                serializer.DataField(ref MinValue, "minValue", -1f);
                serializer.DataField(ref MaxValue, "maxValue", 2f);
                serializer.DataField(ref MinDuration, "minDuration", -1f); 
                serializer.DataField(ref MaxDuration, "maxDuration", 2f);
                ColorsToCycle = serializer.ReadDataField("colorsToCycle", new List<Color>());
            }
        }
    }
}
