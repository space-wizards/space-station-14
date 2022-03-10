using Content.Shared.Disease;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    ///     Shamelessly modified from the chemistry version.
    ///     Adds a generic status effect to the entity,
    ///     not worrying about things like how to affect the time it lasts for
    ///     or component fields or anything. Just adds a component to an entity
    ///     for a given time. Easy.
    /// </summary>
    /// <remarks>
    ///     Can be used for things like adding accents or something. I don't know. Go wild.
    /// </remarks>
    [UsedImplicitly]
    public sealed class DiseaseGenericStatusEffect : DiseaseEffect
    {
        [DataField("key", required: true)]
        public string Key = default!;

        [DataField("component")]
        public string Component = String.Empty;

        [DataField("time")]
        public float Time = 2.0f;

        /// <remarks>
        ///     true - refresh status effect time,  false - accumulate status effect time
        /// </remarks>
        [DataField("refresh")]
        public bool Refresh = true;

        /// <summary>
        ///     Should this effect add the status effect, remove time from it, or set its cooldown?
        /// </summary>
        [DataField("type")]
        public StatusEffectDiseaseType Type = StatusEffectDiseaseType.Add;

        public override void Effect(DiseaseEffectArgs args)
        {
            var statusSys = EntitySystem.Get<StatusEffectsSystem>();
            if (Type == StatusEffectDiseaseType.Add && Component != String.Empty)
            {
                statusSys.TryAddStatusEffect(args.DiseasedEntity, Key, TimeSpan.FromSeconds(Time), Refresh, Component);
            }
            else if (Type == StatusEffectDiseaseType.Remove)
            {
                statusSys.TryRemoveTime(args.DiseasedEntity, Key, TimeSpan.FromSeconds(Time));
            }
            else if (Type == StatusEffectDiseaseType.Set)
            {
                statusSys.TrySetTime(args.DiseasedEntity, Key, TimeSpan.FromSeconds(Time));
            }
        }
    }

    public enum StatusEffectDiseaseType
    {
        Add,
        Remove,
        Set
    }
}
