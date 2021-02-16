#nullable enable
using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components
{
    public partial class LightBehaviourComponentData
    {
        [DataClassTarget("animations")]
        public List<LightBehaviourComponent.AnimationContainer>? Animations;

        public void ExposeData(ObjectSerializer serializer)
        {
            var behaviours = serializer.ReadDataField("behaviours", new List<LightBehaviourAnimationTrack>());
            var key = 0;

            Animations = new List<LightBehaviourComponent.AnimationContainer>();
            foreach (var behaviour in behaviours)
            {
                var animation = new Animation()
                {
                    AnimationTracks = { behaviour }
                };

                Animations.Add(new LightBehaviourComponent.AnimationContainer(key, animation, behaviour));
                key++;
            }

            if (Animations.Count == 0) Animations = null;
        }
    }
}
