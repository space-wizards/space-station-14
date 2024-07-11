using Content.Server.Hallucinations;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// That looks like GenericStatusEffect but with hallucinations pack selection
    /// </summary>
    public sealed partial class HallucinationsReagentEffect : ReagentEffect
    {
        [DataField("key")]
        public string Key = "Hallucinations";

        [DataField(required: true)]
        public string Proto = String.Empty;

        [DataField]
        public float Time = 2.0f;

        [DataField]
        public bool Refresh = true;

        [DataField]
        public HallucinationsMetabolismType Type = HallucinationsMetabolismType.Add;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return Loc.GetString("reagent-effect-guidebook-status-effect",
            ("chance", Probability),
            ("type", Type),
            ("time", Time),
            ("key", $"reagent-effect-status-effect-{Key}"));
        }

        public override void Effect(ReagentEffectArgs args)
        {
            var statusSys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();
            var hallucinationsSys = args.EntityManager.EntitySysManager.GetEntitySystem<HallucinationsSystem>();

            var time = Time;
            time *= args.Scale;

            if (Type == HallucinationsMetabolismType.Add)
            {
                if (!hallucinationsSys.StartHallucinations(args.SolutionEntity, Key, TimeSpan.FromSeconds(Time), Refresh, Proto))
                    return;
            }
            else if (Type == HallucinationsMetabolismType.Remove)
            {
                statusSys.TryRemoveTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(time));
            }
            else if (Type == HallucinationsMetabolismType.Set)
            {
                statusSys.TrySetTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(time));
            }
        }
    }
    public enum HallucinationsMetabolismType
    {
        Add,
        Remove,
        Set
    }
}
