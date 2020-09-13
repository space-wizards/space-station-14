using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedMechanismBehavior))]
    public class BrainComponent : MechanismComponent
    {
        public override string Name => "Brain";
    }
}
