using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [NetworkedComponent()]
    public abstract class SharedHandcuffComponent : Component
    {
        [Serializable, NetSerializable]
        protected sealed class HandcuffedComponentState : ComponentState
        {
            public string? IconState { get; }

            public HandcuffedComponentState(string? iconState)
            {
                IconState = iconState;
            }
        }
    }
}
