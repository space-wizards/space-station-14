using Content.Shared.Humanoid;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class SexChange : ReagentEffect
{
    /// <summary>
    ///     What sex is the consumer changed to? If not set then swap between male/female.
    /// </summary>
    [DataField("sex")]
    public Sex? NewSex;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-sex-change", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.SolutionEntity, out var appearance))
        {
            var uid = args.SolutionEntity;
            var newSex = NewSex;
            var humanoidAppearanceSystem = args.EntityManager.System<SharedHumanoidAppearanceSystem>();

            if (newSex.HasValue)
            {
                humanoidAppearanceSystem.SetSex(uid, newSex.Value);
                return;
            }

            if (appearance.Sex != Sex.Unsexed)
                humanoidAppearanceSystem.SwapSex(uid);
        }
    }
}
