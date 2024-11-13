using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that is activated by a tool being used on it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATToolUseSystem)), AutoGenerateComponentState]
public sealed partial class XATToolUseComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RequiredTool;

    [DataField, AutoNetworkedField]
    public float Delay = 3;

    [DataField, AutoNetworkedField]
    public float Fuel;
}

[Serializable, NetSerializable]
public sealed partial class XATToolUseDoAfterEvent : DoAfterEvent
{
    public NetEntity Node;

    public XATToolUseDoAfterEvent(NetEntity node)
    {
        Node = node;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
