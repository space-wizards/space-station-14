using Content.Server.Humanoid;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class AdjustSex : EntityEffect
{
    [DataField]
    public Gender Gender;

    [DataField]
    public Sex Sex;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-sex", ("sex", Sex));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out HumanoidAppearanceComponent? appearance))
        {
            appearance.Gender = Gender;
            args.EntityManager.EntitySysManager.GetEntitySystem<SharedHumanoidAppearanceSystem>()
                .SetSex(args.TargetEntity, Sex, true, appearance);
        }
    }
}
