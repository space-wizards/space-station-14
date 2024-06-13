using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public sealed partial class AgentIDCardComponent : Component
    {
        /// <summary>
        /// Set of job icons that the agent ID card can show.
        /// </summary>
        [DataField]
        public HashSet<ProtoId<StatusIconPrototype>> Icons;
    }
}
