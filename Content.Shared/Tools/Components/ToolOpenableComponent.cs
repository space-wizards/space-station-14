using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Logic for using tools (or verbs) to open / close something on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToolOpenableComponent : Component
{
    /// <summary>
    /// Is the openable part open or closed?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOpen = false;

    /// <summary>
    /// If a tool is needed to open the entity, the time needed to open the entity in seconds.
    /// </summary>
    [DataField]
    public float OpenTime = 1f;

    /// <summary>
    /// If a tool is needed to close the entity, the time needed to close the entity in seconds.
    /// </summary>
    [DataField]
    public float CloseTime = 1f;

    /// <summary>
    /// The quality of the tool needed to open this.
    /// If null, it will only be openable by a verb.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype>? OpenToolQualityNeeded;

    /// <summary>
    /// The quality of the tool needed to close this.
    /// If null, it will only be closable by a verb.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype>? CloseToolQualityNeeded;

    /// <summary>
    /// If true, verbs will appear to help interact with opening / closing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasVerbs = true;

    /// <summary>
    /// If true, the only way to interact is with verbs. Clicking on the entity will not do anything.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool VerbOnly;

    /// <summary>
    /// The name of what is being opened/closed.
    /// e.g toilet lid, panel, compartment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Name;
}

/// <summary>
/// Simple do after event for opening or closing.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ToolOpenableDoAfterEventToggleOpen : SimpleDoAfterEvent
{
}

/// <summary>
/// AppearanceData keys for use with <see cref="ToolOpenableComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public enum ToolOpenableVisuals : byte
{
    /// <summary><see cref="ToolOpenableVisualState"/>: whether the tool-openable stash is open or closed.</summary>
    ToolOpenableVisualState,
}

/// <summary>
/// The state of a given tool openable entity.
/// </summary>
[Serializable, NetSerializable]
public enum ToolOpenableVisualState : byte
{
    Open,
    Closed
}
