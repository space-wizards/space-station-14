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
    /// <summary>
    /// Tool to be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RequiredTool;

    /// <summary>
    /// Time that using tool on artifact will take.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 3;

    /// <summary>
    /// Amount of fuel using tool will take (for devices such as Welding tool).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Fuel;
}

/// <summary> Do after that will be used if proper tool was used on artifact with <see cref="XATToolUseComponent"/>. </summary>
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
