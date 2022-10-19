using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Polymorph
{
    /// <summary>
    /// Polymorphs generally describe any type of transformation that can be applied to an entity.
    /// </summary>
    [Prototype("polymorph")]
    [DataDefinition]
    public readonly record struct PolymorphPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PolymorphPrototype>))]
        public string[]? Parents { get; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; }

        /// <summary>
        /// What entity the polymorph will turn the target into
        /// must be in here because it makes no sense if it isn't
        /// </summary>
        [DataField("entity", required: true, serverOnly: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public readonly string Entity = string.Empty;

        /// <summary>
        /// The delay between the polymorph's uses in seconds
        /// Slightly weird as of right now.
        /// </summary>
        [DataField("delay", serverOnly: true)] public readonly int Delay = 60;

        /// <summary>
        /// The duration of the transformation in seconds
        /// can be null if there is not one
        /// </summary>
        [DataField("duration", serverOnly: true)]
        public readonly int? Duration;

        /// <summary>
        /// whether or not the target can transform as will
        /// set to true for things like polymorph spells and curses
        /// </summary>
        [DataField("forced", serverOnly: true)]
        public readonly bool Forced;

        /// <summary>
        /// Whether or not the entity transfers its damage between forms.
        /// </summary>
        [DataField("transferDamage", serverOnly: true)]
        public readonly bool TransferDamage = true;

        /// <summary>
        /// Whether or not the entity transfers its name between forms.
        /// </summary>
        [DataField("transferName", serverOnly: true)]
        public readonly bool TransferName;

        /// <summary>
        /// Whether or not the entity transfers its hair, skin color, hair color, etc.
        /// </summary>
        [DataField("transferHumanoidAppearance", serverOnly: true)]
        public readonly bool TransferHumanoidAppearance;

        /// <summary>
        /// Whether or not the entity transfers its inventory and equipment between forms.
        /// </summary>
        [DataField("inventory", serverOnly: true)]
        public readonly PolymorphInventoryChange Inventory = PolymorphInventoryChange.None;

        /// <summary>
        /// Whether or not the polymorph reverts when the entity goes into crit.
        /// </summary>
        [DataField("revertOnCrit", serverOnly: true)]
        public readonly bool RevertOnCrit = true;

        /// <summary>
        /// Whether or not the polymorph reverts when the entity dies.
        /// </summary>
        [DataField("revertOnDeath", serverOnly: true)]
        public readonly bool RevertOnDeath = true;

        [DataField("allowRepeatedMorphs", serverOnly: true)]
        public readonly bool AllowRepeatedMorphs;
    }

    public enum PolymorphInventoryChange : byte
    {
        None,
        Drop,
        Transfer,
    }
}
