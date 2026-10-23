using Content.Shared.Lock.BypassLock.Systems;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lock.BypassLock.Components;

/// <summary>
/// This component lets the lock on this entity be pried open when the entity is in critical or dead state.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BypassLockSystem))]
public sealed partial class BypassLockComponent : Component
{
    /// <summary>
    /// The tool quality needed to bypass the lock.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> BypassingTool = "Prying";

    /// <summary>
    /// Amount of time in seconds it takes to bypass
    /// </summary>
    [DataField]
    public TimeSpan BypassDelay = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Whether the wirepanel should be opened as well, if one exists.
    /// </summary>
    [DataField]
    public bool OpenWiresPanel = false;
}
