using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Polymorph;

/// <summary>
/// Polymorphs generally describe any type of transformation that can be applied to an entity.
/// </summary>
[Prototype]
[DataDefinition]
public sealed partial class PolymorphPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PolymorphPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true, serverOnly: true)]
    public PolymorphConfiguration Configuration = new();

}

/// <summary>
/// Defines information about the polymorph
/// </summary>
[DataDefinition]
public sealed partial record PolymorphConfiguration
{
    /// <summary>
    /// What entity the polymorph will turn the target into
    /// must be in here because it makes no sense if it isn't
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public EntProtoId Entity;

    /// <summary>
    /// The delay between the polymorph's uses in seconds
    /// Slightly weird as of right now.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Delay = 60;

    /// <summary>
    /// The duration of the transformation in seconds
    /// can be null if there is not one
    /// </summary>
    [DataField(serverOnly: true)]
    public int? Duration;

    /// <summary>
    /// whether or not the target can transform as will
    /// set to true for things like polymorph spells and curses
    /// </summary>
    [DataField(serverOnly: true)]
    public bool Forced;

    /// <summary>
    /// Whether or not the entity transfers its damage between forms.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool TransferDamage = true;

    /// <summary>
    /// Whether or not the entity transfers its name between forms.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool TransferName;

    /// <summary>
    /// Whether or not the entity transfers its hair, skin color, hair color, etc.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool TransferHumanoidAppearance;

    /// <summary>
    /// Whether or not the entity transfers its inventory and equipment between forms.
    /// </summary>
    [DataField(serverOnly: true)]
    public PolymorphInventoryChange Inventory = PolymorphInventoryChange.None;

    /// <summary>
    /// Whether or not the polymorph reverts when the entity goes into crit.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool RevertOnCrit = true;

    /// <summary>
    /// Whether or not the polymorph reverts when the entity dies.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool RevertOnDeath = true;

    /// <summary>
    /// Whether or not the polymorph reverts when the entity is eaten or fully sliced.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool RevertOnEat;

    /// <summary>
    /// Whether or not an already polymorphed entity is able to be polymorphed again
    /// </summary>
    [DataField(serverOnly: true)]
    public bool AllowRepeatedMorphs;

    /// <summary>
    /// The amount of time that should pass after this polymorph has ended, before a new one
    /// can occur.
    /// </summary>
    [DataField(serverOnly: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    ///     If not null, this sound will be played when being polymorphed into something.
    /// </summary>
    [DataField]
    public SoundSpecifier? PolymorphSound;

    /// <summary>
    ///     If not null, this sound will be played when being reverted from a polymorph.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExitPolymorphSound;
}

public enum PolymorphInventoryChange : byte
{
    None,
    Drop,
    Transfer,
}
