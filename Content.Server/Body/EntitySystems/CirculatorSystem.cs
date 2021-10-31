using Content.Server.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    public class CirculatorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(MetabolizerSystem));
        }

        public override void Update(float frameTime)
        {
            foreach (var circ in EntityManager.EntityQuery<CirculatorComponent>())
            {
                circ.AccumulatedFrametime += frameTime;

                // TODO MIRROR Move/accept/process bloodstream reagents only when the heart is pumping
                if (circ.AccumulatedFrametime >= circ.HeartRate)
                {
                    // bloodstream.Update(_accumulatedFrameTime);
                    circ.AccumulatedFrametime -= circ.HeartRate;
                }
            }
        }
    }
}
