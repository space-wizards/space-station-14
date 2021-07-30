using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Shared.Body.Networks;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Behavior
{
    /// <summary>
    /// Metabolizes reagents in <see cref="SharedBloodstreamComponent"/> after they are digested.
    /// </summary>
    public class LiverBehavior : MechanismBehavior
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            if (Body == null)
            {
                return;
            }

            _accumulatedFrameTime += frameTime;

            // Update at most once per second
            if (_accumulatedFrameTime < 1)
            {
                return;
            }

            _accumulatedFrameTime -= 1;
        }
    }
}
