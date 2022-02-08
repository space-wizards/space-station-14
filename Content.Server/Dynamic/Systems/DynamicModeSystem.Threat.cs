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
        // This shouldn't actually happen.
        if (CurrentStoryteller == null)
        {
            Logger.Error("Dynamic tried to generate threat without a storyteller!");
            return;
        }

        var st = CurrentStoryteller;
        var curveCenterMod = st.RoundstartCurveCenterModifier.Choose(_random);
        var curveWidthMod = st.RoundstartCurveWidthModifier.Choose(_random);
        var threatCapMod = st.ThreatCapModifier.Choose(_random);

        var relativeThreat =
            Lorentz.ProbabilityDensity(ThreatCurveCenter + curveCenterMod, ThreatCurveWidth + curveWidthMod);
        ThreatLevel = Math.Clamp(Lorentz.LorentzToAmount(relativeThreat), 0, MaxThreatLevel + threatCapMod);
    }

    /// <summary>
    ///     Fills in the roundstart and midround budgets using threat and the split curve.
    /// </summary>
    public void GenerateBudgets()
    {
        // This shouldn't actually happen.
        if (CurrentStoryteller == null)
        {
            Logger.Error("Dynamic tried to generate budgets without a storyteller!");
            return;
        }

        var st = CurrentStoryteller;
        var curveCenterMod = st.SplitCurveCenterModifier.Choose(_random);
        var curveWidthMod = st.SplitCurveWidthModifier.Choose(_random);

        var relativeRoundstartThreatScale =
            Lorentz.ProbabilityDensity(SplitCurveCenter + curveCenterMod, SplitCurveWidth + curveWidthMod);

        RoundstartBudget = (Lorentz.LorentzToAmount(relativeRoundstartThreatScale) / 100.0f) * ThreatLevel;
        RoundstartBudget = MathF.Round(RoundstartBudget, 2);
        MidroundBudget = ThreatLevel - RoundstartBudget;
    }
}
