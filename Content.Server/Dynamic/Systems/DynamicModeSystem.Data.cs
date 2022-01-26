using System;
using Content.Shared.CCVar;
using Robust.Shared.ViewVariables;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     Threat purchase should generally not exceed this number.
    /// </summary>
    [ViewVariables]
    public int ThreatCap;

    /// <summary>
    ///     The total threat level.
    /// </summary>
    [ViewVariables]
    public float ThreatLevel;

    /// <summary>
    ///     Max threat level allowed to generate.
    /// </summary>
    [ViewVariables]
    public float MaxThreatLevel => _cfg.GetCVar(CCVars.DynamicMaxThreat);

    /// <summary>
    ///     "Spent" by dynamic at roundstart to select events.
    /// </summary>
    [ViewVariables]
    public float RoundstartBudget;

    /// <summary>
    ///     "Spent" by dynamic during the round to select events.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MidroundBudget;

    #region Curves

    /// <summary>
    ///     A number between -5 and 5.
    /// </summary>
    [ViewVariables]
    public float ThreatCurveCenter = 0f;

    [ViewVariables]
    public float ThreatCurveWidth = 0f;

    [ViewVariables]
    public float SplitCurveCenter = 0f;

    [ViewVariables]
    public float SplitCurveWidth = 0f;

    #endregion

    #region Latejoin

    private float _latejoinAccumulator;

    /// <summary>
    ///     When will dynamic start accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinStart => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectStart));

    /// <summary>
    ///     When will dynamic stop accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinEnd => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectEnd));

    #endregion

    #region Midround

    private float _midroundAccumulator;

    /// <summary>
    ///     When will dynamic start accepting latejoin events?
    /// </summary>
    public TimeSpan MidroundStart => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicMidroundStart));

    /// <summary>
    ///     When will dynamic stop accepting latejoin events?
    /// </summary>
    public TimeSpan MidroundEnd => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicMidroundEnd));

    #endregion
}
