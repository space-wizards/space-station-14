using Content.Shared.Disease;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Adds a generic status effect to the entity.
    /// Differs from the chem version in its defaults
    /// to better facilitate adding components that
    /// last the length of the disease.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseGenericStatusEffect : DiseaseEffect
    {
        /// <summary>
        /// The status effect key
        /// Prevents other components from being with the same key
        /// </summary>
        [DataField("key", required: true)]
        public string Key = default!;
        /// <summary>
        /// The component to add
        /// </summary>
        [DataField("component")]
        public string Component = String.Empty;

        [DataField("time")]
        public float Time = 1.01f; /// I'm afraid if this was exact the key could get stolen by another thing

        /// <remarks>
        ///     true - refresh status effect time,  false - accumulate status effect time
        /// </remarks>
        [DataField("refresh")]
        public bool Refresh = false;

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
    /// See status effects for how these work
    public enum StatusEffectDiseaseType
    {
        Add,
        Remove,
        Set
    }
}
