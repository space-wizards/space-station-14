using System.ComponentModel.DataAnnotations;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization;
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
    public string PassiveBlockDamageModifer = default!;

    /// <summary>
    /// The damage modifier to use while actively blocking.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activeBlockModifier")]
    public string ActiveBlockDamageModifier = default!;

    [DataField("blockingToggleActionId", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string BlockingToggleActionId = "ToggleBlock";

    [DataField("blockingToggleAction")]
    public InstantAction? BlockingToggleAction;
}
