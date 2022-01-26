using System;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
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
        MidroundBudget = ThreatLevel - RoundstartBudget;
    }
}
