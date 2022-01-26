using System;
using Content.Shared.CCVar;
using Robust.Shared.ViewVariables;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     The total threat level. Threat purchases should not exceed this number.
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

    /// <summary>
    ///     A number between -5 and 5.
    /// </summary>
    [ViewVariables]
    public float ThreatCurveCenter = 0f;

    [ViewVariables]
    public float ThreatCurveWidth = 1.8f;

    [ViewVariables]
    public float SplitCurveCenter = 1f;

    [ViewVariables]
    public float SplitCurveWidth = 1.8f;

    [ViewVariables]
    public bool AddedHighImpact = false;

    /// <summary>
    ///     Fills in the threat pool using a Lorentz distribution.
    /// </summary>
    public void GenerateThreat()
    {
        var relativeThreat = Lorentz.ProbabilityDensity(ThreatCurveCenter, ThreatCurveWidth);
        ThreatLevel = Math.Clamp(Lorentz.LorentzToAmount(relativeThreat), 0, MaxThreatLevel);
    }

    /// <summary>
    ///     Fills in the roundstart and midround budgets using threat and the split curve.
    /// </summary>
    public void GenerateBudgets()
    {
        var relativeRoundstartThreatScale = Lorentz.ProbabilityDensity(SplitCurveCenter, SplitCurveWidth);
        RoundstartBudget = (Lorentz.LorentzToAmount(relativeRoundstartThreatScale) / 100.0f) * ThreatLevel;
        RoundstartBudget = MathF.Round(RoundstartBudget, 2);
        MidroundBudget = ThreatLevel - RoundstartBudget;
    }
}
