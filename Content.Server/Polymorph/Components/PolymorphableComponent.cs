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

        /// <summary>
        /// A list of all the polymorphs that the entity has.
        /// Used to manage them and remove them if needed.
        /// </summary>
        [DataField("polymorphActions")]
        public Dictionary<string, InstantAction> PolymorphActions = new();

        /// <summary>
        /// The polymorphs that the entity starts out being able to do.
        /// </summary>
        [DataField("innatePolymorphs")]
        public List<string>? InnatePolymorphs = null;
    }
}
