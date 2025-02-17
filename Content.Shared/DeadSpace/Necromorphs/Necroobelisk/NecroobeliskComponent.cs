// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Necromorphs.Necroobelisk;

[RegisterComponent, NetworkedComponent, EntityCategory("Spawner")]
public sealed partial class NecroobeliskComponent : Component
{
    #region Sanity

    [DataField("rangesanity")]
    [ViewVariables(VVAccess.ReadOnly)]
    public float RangeSanity = 30f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CheckDurationSanity = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextCheckTimeSanity = TimeSpan.Zero;

    #endregion

    #region Pulse

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPulseTime = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilPulse = TimeSpan.FromSeconds(15);

    [DataField]
    public float SanityDamage = 3;

    [DataField("mobsForStageConvergence")]

    [ViewVariables(VVAccess.ReadWrite)]
    public int MobsForStageConvergence = 15;

    [ViewVariables(VVAccess.ReadOnly)]
    public int MobsAbsorbed = 0;

    #endregion

    #region Visualizer

    [DataField("state")]
    public string State = "active";

    [DataField("unactiveState")]
    public string UnactiveState = "unactive";

    #endregion

    #region Sounds

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SoundDestruction = "/Audio/_DeadSpace/Necromorfs/unitolog_start.ogg";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SoundConvergence = "/Audio/_DeadSpace/Necromorfs/marker_convergence.ogg";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SoundInit = "/Audio/_DeadSpace/Necromorfs/marker_convergence.ogg";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Sound = "/Audio/_DeadSpace/Necromorfs/marker_red.ogg";

    #endregion

    #region Bool

    [DataField("warn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsGivesWarnings = true;

    [DataField("stoper")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsStoper = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsStageConvergence = false;

    [DataField("active")]
    public bool IsActive = true;

    [DataField("canConvergence")]
    public bool IsCanStartConvergence = true;

    [DataField("cudzu")]
    public bool SpawnCudzu = true;

    #endregion
}

[Serializable, NetSerializable]
public sealed class NecroobeliskComponentState : ComponentState
{
    public TimeSpan NextPulseTime;
}

[ByRefEvent]
public readonly record struct NecroobeliskPulseEvent();

[ByRefEvent]
public readonly record struct NecroobeliskStartConvergenceEvent();

[ByRefEvent]
public readonly record struct NecroMoonAppearanceEvent();

[ByRefEvent]
public readonly record struct NecroobeliskAbsorbEvent(EntityUid Target);

//[ByRefEvent]
//public readonly record struct NecroobeliskSpawnArmyEvent();

[Serializable, NetSerializable]
public sealed class SanityComponentState : ComponentState
{
    public TimeSpan NextCheckTime;
}

[ByRefEvent]
public readonly record struct SanityLostEvent(EntityUid VictinUID);
