using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>Remembers the donor of a reagent as a leader, friendly entity, or combat target.</summary>
public sealed partial class DnaImprint : EntityEffectBase<DnaImprint>
{

    /// <summary>
    /// The reagent this effect is being applied from (needed to find the DNA on it in their bloodstream).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Whether this reagent marks the DNA as leader.
    /// </summary>
    [DataField]
    public bool Leader;

    /// <summary>
    /// Whether this reagent marks the DNA as friendly.
    /// </summary>
    [DataField]
    public bool Friendly;

    /// <summary>
    /// Whether this reagent marks the DNA as hostile.
    /// </summary>
    [DataField]
    public bool Target;

    [DataField(required: true)]
    public LocId GuidebookText;

    public override LogImpact? Impact => LogImpact.High;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}
