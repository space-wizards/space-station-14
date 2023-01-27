using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [NetworkedComponent()]
    public abstract partial class SharedHandcuffComponent : Component
    {
        [Serializable, NetSerializable]
        protected sealed partial class HandcuffedComponentState : ComponentState
        {
            public string? IconState { get; }

            public HandcuffedComponentState(string? iconState)
            {
                IconState = iconState;
            }
        }
    }
}
