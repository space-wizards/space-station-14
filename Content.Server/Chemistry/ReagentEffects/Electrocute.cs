using Content.Server.Electrocution;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class Electrocute : ReagentEffect
{
    [DataField] public int ElectrocuteTime = 2;

    [DataField] public int ElectrocuteDamageScale = 5;

    /// <remarks>
    ///     true - refresh electrocute time,  false - accumulate electrocute time
    /// </remarks>
    [DataField] public bool Refresh = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-electrocute", ("chance", Probability), ("time", ElectrocuteTime));

    public override bool ShouldLog => true;

    public override void Effect(ReagentEffectArgs args)
    {
        args.EntityManager.System<ElectrocutionSystem>().TryDoElectrocution(args.SolutionEntity, null,
            Math.Max((args.Quantity * ElectrocuteDamageScale).Int(), 1), TimeSpan.FromSeconds(ElectrocuteTime), Refresh, ignoreInsulation: true);

        if (args.Reagent != null)
            args.Source?.RemoveReagent(args.Reagent.ID, args.Quantity);
    }
}
