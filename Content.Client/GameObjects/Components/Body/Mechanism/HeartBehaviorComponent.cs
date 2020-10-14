using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Mechanism
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHeartBehaviorComponent))]
    public class HeartBehaviorComponent : SharedHeartBehaviorComponent
    {
        public override void Update(float frameTime) { }
    }
}
