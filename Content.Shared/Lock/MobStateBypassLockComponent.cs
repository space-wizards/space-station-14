using Content.Shared.Mobs;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lock;

[RegisterComponent, NetworkedComponent, Access(typeof(MobStateBypassLockSystem))]
public sealed partial class MobStateBypassLockComponent : Component
{
    /// <summary>
    /// The tool quality needed to bypass the lock.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> BypassingTool = "Prying";

    /// <summary>
    /// The mobstate where the ID lock can be bypassed.
    /// </summary>
    [DataField]
    public MobState RequiredMobState = MobState.Critical;

    /// <summary>
    /// Amount of time in seconds it takes to bypass
    /// </summary>
    [DataField]
    public TimeSpan BypassDelay = TimeSpan.FromSeconds(3);
}
