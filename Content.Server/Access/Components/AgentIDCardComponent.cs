using Content.Shared.StatusIcon;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public sealed class AgentIDCardComponent : Component
    {
        /// <summary>
        /// Set of job icons that the agent ID card can show.
        /// </summary>
        [DataField("icons", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StatusIconPrototype>))]
        public readonly HashSet<string> Icons = new();
    }
}
