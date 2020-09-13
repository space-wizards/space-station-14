using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(IMechanismBehavior))]
    public class BrainBehaviorComponent : MechanismBehaviorComponent
    {
        public override string Name => "Brain";

        public override void Update(float frameTime)
        {
            // TODO
        }
    }
}
