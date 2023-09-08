using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Blocking;

/// <summary>
/// This component goes on an item that you want to use to block
/// </summary>
[RegisterComponent]
public sealed partial class BlockingComponent : Component
{
    /// <summary>
    /// The entity that's blocking
    /// </summary>
    [ViewVariables]
    public EntityUid? User;

    /// <summary>
    /// Is it currently blocking?
    /// </summary>
    [ViewVariables]
    public bool IsBlocking;

    /// <summary>
    /// The ID for the fixture that's dynamically created when blocking
    /// </summary>
    public const string BlockFixtureID = "blocking-active";

    /// <summary>
    /// The shape of the blocking fixture that will be dynamically spawned
    /// </summary>
    [DataField("shape"), ViewVariables(VVAccess.ReadWrite)]
    public IPhysShape Shape = new PhysShapeCircle(0.5f);

    /// <summary>
    /// The damage modifer to use while passively blocking
    /// </summary>
    [DataField("passiveBlockModifier", required: true)]
    public DamageModifierSet PassiveBlockDamageModifer = default!;

    /// <summary>
    /// The damage modifier to use while actively blocking.
    /// </summary>
    [DataField("activeBlockModifier", required: true)]
    public DamageModifierSet ActiveBlockDamageModifier = default!;

    [DataField("blockingToggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BlockingToggleAction = "ActionToggleBlock";

    [DataField("blockingToggleActionEntity")]
    public EntityUid? BlockingToggleActionEntity;

    /// <summary>
    /// The sound to be played when you get hit while actively blocking
    /// </summary>
    [DataField("blockSound")]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg");

    /// <summary>
    /// Fraction of original damage shield will take instead of user
    /// when not blocking
    /// </summary>
    [DataField("passiveBlockFraction"), ViewVariables(VVAccess.ReadWrite)]
    public float PassiveBlockFraction = 0.5f;

    /// <summary>
    /// Fraction of original damage shield will take instead of user
    /// when blocking
    /// </summary>
    [DataField("activeBlockFraction"), ViewVariables(VVAccess.ReadWrite)]
    public float ActiveBlockFraction = 1.0f;
}
