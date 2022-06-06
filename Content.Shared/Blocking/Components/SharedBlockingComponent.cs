using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Blocking;

[RegisterComponent]
public sealed class SharedBlockingComponent : Component
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
    public string BlockFixtureID = "blocking-active";

    /// <summary>
    /// The shape of the blocking fixture that will be dynamically spawned
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("blockRadius")]
    public float BlockRadius = 0.5f;

    /// <summary>
    /// The damage modifer to use while passively blocking
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("passiveBlockModifier")]
    public string PassiveBlockDamageModifer = "Metallic";

    /// <summary>
    /// The damage modifier to use while actively blocking.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activeBlockModifier")]
    public string ActiveBlockDamageModifier = "Metallic";

    [DataField("blockingToggleActionId", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string BlockingToggleActionId = "ToggleBlock";

    [DataField("blockingToggleAction")]
    public InstantAction? BlockingToggleAction;

    /// <summary>
    /// The sound to be played when you get hit while actively blocking
    /// </summary>
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg");
}
