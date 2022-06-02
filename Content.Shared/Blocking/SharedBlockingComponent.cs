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
