using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AI
{
    [NetworkedComponent()]
    public abstract class SharedAIComponent : Component
    {
        /// <summary>
        /// Does AI uses holopad right now?
        /// </summary>
        [DataField("isHolopad")]
        private bool IsHolopad;
    }
}
