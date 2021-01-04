using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components
{
    public partial class LightBehaviourComponentData
    {
        [CustomYamlField("animations")]
        public List<LightBehaviourComponent.AnimationContainer> _animations = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var behaviours = serializer.ReadDataField("behaviours", new List<LightBehaviourAnimationTrack>());
            var key = 0;

            foreach (var behaviour in behaviours)
            {
                var animation = new Animation()
                {
                    AnimationTracks = { behaviour }
                };

                _animations.Add(new LightBehaviourComponent.AnimationContainer(key, animation, behaviour));
                key++;
            }
        }
    }
}
