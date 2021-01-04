using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.GameObjects.Components.Mobs.State
{
    public class CriticalMobState : SharedCriticalMobState
    {
        public override IDeepClone DeepClone()
        {
            return new CriticalMobState();
        }
    }
}
