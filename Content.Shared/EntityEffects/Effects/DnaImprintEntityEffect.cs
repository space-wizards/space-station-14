using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>Remembers the donor of a reagent as a leader, friendly entity, or combat target.</summary>
public sealed partial class DnaImprint : EntityEffectBase<DnaImprint>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public bool Leader;

    [DataField]
    public bool Friendly;

    [DataField]
    public bool Target;

    [DataField(required: true)]
    public LocId GuidebookText;

    public override LogImpact? Impact => LogImpact.High;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}
