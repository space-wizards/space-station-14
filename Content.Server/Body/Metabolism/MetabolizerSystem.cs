using Robust.Shared.GameObjects;

namespace Content.Server.Body.Metabolism
{
    // TODO mirror move updating here to BodySystem so it can be ordered?
    public class MetabolizerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var metab in ComponentManager.EntityQuery<MetabolizerComponent>(false))
            {
                metab.AccumulatedFrametime += frameTime;

                // Only update as frequently as it should
                if (metab.AccumulatedFrametime >= metab.UpdateFrequency)
                {
                    metab.AccumulatedFrametime = 0.0f;
                    TryMetabolize(metab);
                }
            }
        }

        private void TryMetabolize(MetabolizerComponent comp)
        {

        }
    }
}
