using Content.Shared.Chemistry.Reagent;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects.StatusEffects
{
    /// <summary>
    ///     Adds a generic status effect to the entity,
    ///     not worrying about things like how to affect the time it lasts for
    ///     or component fields or anything. Just adds a component to an entity
    ///     for a given time. Easy.
    /// </summary>
    /// <remarks>
    ///     Can be used for things like adding accents or something. I don't know. Go wild.
    /// </remarks>
    [UsedImplicitly]
    public sealed partial class GenericStatusEffect : ReagentEffect
    {
        [DataField(required: true)]
        public string Key = default!;

        [DataField]
        public string Component = String.Empty;

        [DataField]
        public float Time = 2.0f;

        /// <remarks>
        ///     true - refresh status effect time,  false - accumulate status effect time
        /// </remarks>
        [DataField]
        public bool Refresh = true;

        /// <summary>
        ///     Should this effect add the status effect, remove time from it, or set its cooldown?
        /// </summary>
        [DataField]
        public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Add;

        public override void Effect(ReagentEffectArgs args)
        {
            var statusSys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();

            var time = Time;
            time *= args.Scale;

            if (Type == StatusEffectMetabolismType.Add && Component != String.Empty)
            {
                statusSys.TryAddStatusEffect(args.SolutionEntity, Key, TimeSpan.FromSeconds(time), Refresh, Component);
            }
            else if (Type == StatusEffectMetabolismType.Remove)
            {
                statusSys.TryRemoveTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(time));
            }
            else if (Type == StatusEffectMetabolismType.Set)
            {
                statusSys.TrySetTime(args.SolutionEntity, Key, TimeSpan.FromSeconds(time));
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString(
            "reagent-effect-guidebook-status-effect",
            ("chance", Probability),
            ("type", Type),
            ("time", Time),
            ("key", $"reagent-effect-status-effect-{Key}"));
    }

    public enum StatusEffectMetabolismType
    {
        Add,
        Remove,
        Set
    }
}
