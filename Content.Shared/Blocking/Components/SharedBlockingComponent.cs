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
    [DataField("blockRadius")]
    public float BlockRadius = 0.5f;

    [DataField("blockingToggleActionId", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string BlockingToggleActionId = "ToggleBlock";

    [DataField("blockingToggleAction")]
    public InstantAction? BlockingToggleAction;
}

[Serializable, NetSerializable]
public sealed class BlockingComponentState : ComponentState
{
    public bool Blocking { get; }

    public BlockingComponentState(bool blocking)
    {
        Blocking = blocking;
    }
}

public sealed class BlockingEvent : EntityEventArgs
{

}
