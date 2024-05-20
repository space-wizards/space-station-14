using Content.Server.Electrocution;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class Electrocute : EntityEffect
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

    public override void Effect(EntityEffectArgs args)
    {
        args.EntityManager.System<ElectrocutionSystem>().TryDoElectrocution(args.TargetEntity, null,
            Math.Max((args.Quantity * ElectrocuteDamageScale).Int(), 1), TimeSpan.FromSeconds(ElectrocuteTime), Refresh, ignoreInsulation: true);

        if (args.Reagent != null)
            args.Source?.RemoveReagent(args.Reagent.ID, args.Quantity);
    }
}
