using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Emoting;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmotingComponent : Component
{
   [DataField("enabled"), Access(typeof(EmoteSystem),
        Friend = AccessPermissions.ReadWrite,
        Other = AccessPermissions.Read)] public bool Enabled = true;
        
   /// <summary>
    /// Open emotes action id
    /// </summary>
    [DataField("actionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ActionId = "OpenEmotes";

    /// <summary>
    /// Used for open emote menu action button
    /// </summary>
    [DataField("action")]
    public InstantAction? Action;
}

[Serializable, NetSerializable]
public sealed class RequestEmoteMenuEvent : EntityEventArgs
{
    public readonly List<string> Prototypes = new();
    public EntityUid Target { get; }

    public RequestEmoteMenuEvent(EntityUid target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class SelectEmoteEvent : EntityEventArgs
{
    public string PrototypeId { get; }
    public EntityUid Target { get; }

    public SelectEmoteEvent(EntityUid target, string prototypeId)
    {
        Target = target;
        PrototypeId = prototypeId;
    }
}

public sealed class OpenEmotesActionEvent : InstantActionEvent
{
}
