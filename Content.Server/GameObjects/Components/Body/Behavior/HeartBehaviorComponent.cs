using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Networks;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHeartBehaviorComponent))]
    public class HeartBehaviorComponent : SharedHeartBehaviorComponent
    {
        private float _accumulatedFrameTime;

        [ViewVariables] private BloodstreamComponent _bloodstream;

        public override void Initialize()
        {
            base.Initialize();

            _bloodstream = Owner.EnsureComponent<BloodstreamComponent>();
        }

        public override void Update(float frameTime)
        {
            // TODO BODY do between pre and metabolism
            if (Mechanism?.Body == null ||
                !Mechanism.Body.Owner.HasComponent<SharedBloodstreamComponent>())
            {
                return;
            }

            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            // TODO: Move/accept/process bloodstream reagents only when the heart is pumping
            if (_accumulatedFrameTime >= 1)
            {
                // bloodstream.Update(_accumulatedFrameTime);
                _bloodstream.Solution.ContainsReagent("chem.Blood", out var reagent);
                if(reagent < ReagentUnit.New(50))
                {
                    _bloodstream.Solution.TryAddReagent("chem.Blood", ReagentUnit.New(.1), out var accepted);
                }

                _accumulatedFrameTime -= 1;
            }
        }
    }
}
