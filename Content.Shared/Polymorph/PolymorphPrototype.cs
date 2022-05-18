using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Polymorph
{
    /// <summary>
    /// Polymorphs generally describe any type of transformation that can be applied to an entity.
    /// </summary>
    [Prototype("polymorph")]
    [DataDefinition]
    public sealed class PolymorphPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ParentDataField(typeof(AbstractPrototypeIdSerializer<PolymorphPrototype>))]
        public string? Parent { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        /// <summary>
        /// What entity the polymorph will turn the target into
        /// must be in here because it makes no sense if it isn't
        /// </summary>
        [DataField("entity", required: true, serverOnly: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Entity = string.Empty;

        /// <summary>
        /// The delay between the polymorph's uses in seconds
        /// Slightly weird as of right now.
        /// </summary>
        [DataField("delay", serverOnly: true)]
        public int Delay = 60;

        /// <summary>
        /// The duration of the transformation in seconds
        /// can be null if there is not one
        /// </summary>
        [DataField("duration", serverOnly: true)]
        public int? Duration = null;

        /// <summary>
        /// whether or not the target can transform as will
        /// set to true for things like polymorph spells and curses
        /// </summary>
        [DataField("forced", serverOnly: true)]
        public bool Forced = false;

        /// <summary>
        /// Whether or not the target will drop their inventory
        /// when they are polymorphed (includes items in hands)
        /// </summary>
        [DataField("dropInventory", serverOnly: true)]
        public bool DropInventory = false;

        /// <summary>
        /// Whether or not the polymorph reverts when the entity goes into crit.
        /// </summary>
        [DataField("revertOnCrit", serverOnly: true)]
        public bool RevertOnCrit = true;

        /// <summary>
        /// Whether or not the polymorph reverts when the entity dies.
        /// </summary>
        [DataField("revertOnDeath", serverOnly: true)]
        public bool RevertOnDeath = true;
    }
}
