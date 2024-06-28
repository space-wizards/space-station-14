using Content.Server.IdentityManagement;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class GenderChange : ReagentEffect
{
    /// <summary>
    ///     What gender is the consumer changed to? If not set then swap between male/female.
    /// </summary>
    [DataField("gender")]
    public Gender? NewGender;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-gender-change", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<GrammarComponent>(args.SolutionEntity, out var grammar))
        {
            var uid = args.SolutionEntity;
            var newGender = NewGender;
            var grammarSystem = args.EntityManager.System<GrammarSystem>();
            var identitySystem = args.EntityManager.System<IdentitySystem>();

            // bleh, this probably should not be here but I have no clue where to put it
            if (grammar.Gender != Gender.Epicene && grammar.Gender != Gender.Neuter)
            {
                if (grammar.Gender == Gender.Male)
                    newGender = Gender.Female;
                else
                    newGender = Gender.Male;
            }

            if (newGender.HasValue)
            {
                grammarSystem.SetGender((uid, grammar), newGender);

                if (args.EntityManager.HasComponent<IdentityComponent>(uid))
                    identitySystem.QueueIdentityUpdate(uid);
            }
        }
    }
}
