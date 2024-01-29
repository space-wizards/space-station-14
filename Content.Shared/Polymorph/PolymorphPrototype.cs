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
    public sealed partial class PolymorphPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PolymorphPrototype>))]
        public string[]? Parents { get; private set; }

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
        /// Whether or not the entity transfers its damage between forms.
        /// </summary>
        [DataField("transferDamage", serverOnly: true)]
        public bool TransferDamage = true;

        /// <summary>
        /// Whether or not the entity transfers its name between forms.
        /// </summary>
        [DataField("transferName", serverOnly: true)]
        public bool TransferName = false;

        /// <summary>
        /// Whether or not the entity transfers its hair, skin color, hair color, etc.
        /// </summary>
        [DataField("transferHumanoidAppearance", serverOnly: true)]
        public bool TransferHumanoidAppearance = false;

        /// <summary>
        /// Whether or not the entity transfers its inventory and equipment between forms.
        /// </summary>
        [DataField("inventory", serverOnly: true)]
        public PolymorphInventoryChange Inventory = PolymorphInventoryChange.None;

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

        /// <summary>
        /// Whether or not the polymorph reverts when the entity is eaten or fully sliced.
        /// </summary>
        [DataField("revertOnEat", serverOnly: true)]
        public bool RevertOnEat = false;

        [DataField("allowRepeatedMorphs", serverOnly: true)]
        public bool AllowRepeatedMorphs = false;

        /// <summary>
        /// The amount of time that should pass after this polymorph has ended, before a new one
        /// can occur.
        /// </summary>
        [DataField("cooldown", serverOnly: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Cooldown = TimeSpan.Zero;
    }

    public enum PolymorphInventoryChange : byte
    {
        None,
        Drop,
        Transfer,
    }
}
