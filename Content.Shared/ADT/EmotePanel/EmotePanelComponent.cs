using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.EmotePanel;

/// <summary>
/// This component describes ActionEntity "ActionOpenEmotes". This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmotePanelComponent : Component
{
    [DataField]
    public EntProtoId OpenEmotesAction = "ActionOpenEmotes";

    [DataField]
    public EntityUid? OpenEmotesActionEntity;
}

/// <summary>
/// This event carries list of emotes-prototypes and entity - the source of request. This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RequestEmoteMenuEvent : EntityEventArgs
{
    public readonly List<string> Prototypes = new();
    public int Target { get; }

    public RequestEmoteMenuEvent(int target)
    {
        Target = target;
    }
}

/// <summary>
/// This event carries prototype-id of emote, which was selected. This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SelectEmoteEvent : EntityEventArgs
{
    public string PrototypeId { get; }
    public int Target { get; }

    public SelectEmoteEvent(int target, string prototypeId)
    {
        Target = target;
        PrototypeId = prototypeId;
    }
}

public sealed partial class OpenEmotesActionEvent : InstantActionEvent { }
