using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Polymorph.Components
{
    [RegisterComponent]
    public sealed class PolymorphableComponent : Component
    {
        /// <summary>
        /// Whether or not the user is currently transformed
        /// </summary>
        [DataField("polymorphed")]
        public bool Polymorphed = false;
    }
}
