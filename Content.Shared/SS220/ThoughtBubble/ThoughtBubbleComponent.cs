// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ThoughtBubble;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class ThoughtBubbleComponent : Component
{
    /// <summary>
    /// How long will bubble appear
    /// </summary>
    [DataField]
    public TimeSpan DurationShow = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Time when bubble will disappear
    /// </summary>
    [DataField]
    public TimeSpan? TimeEndShow;

    [DataField, AutoNetworkedField]
    public EntProtoId BubbleProto = "EffectThoughtBubble";

    [ViewVariables, AutoNetworkedField]
    public NetEntity? PointedItem;

    /// <summary>
    /// Client-side bubble entity
    /// </summary>
    [ViewVariables]
    public EntityUid? BubbleEntity;

    /// <summary>
    /// Client-side bubble entity
    /// </summary>
    [ViewVariables]
    public EntityUid? ShownInBubbleItem;
}

[Serializable, NetSerializable]
public enum ThoughtBubbleVisuals : byte
{
    Bubble,
    Icon
}
