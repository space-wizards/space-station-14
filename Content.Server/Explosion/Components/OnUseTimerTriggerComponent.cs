using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component
    {
        [DataField("delay")] public float Delay = 0f;
    }
}
