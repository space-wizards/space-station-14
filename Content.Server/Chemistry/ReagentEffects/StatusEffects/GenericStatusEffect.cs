using System;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

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
    public class GenericStatusEffect : ReagentEffect
    {
        [DataField("key", required: true)]
        public string Key = default!;

        [DataField("component", required: true)]
        public string Component = default!;

        [DataField("time")]
        public float Time = 2.0f;

        public override void Metabolize(EntityUid solutionEntity, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            entityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>()
                .TryAddStatusEffect(solutionEntity, Key, TimeSpan.FromSeconds(Time), Component);
        }
    }
}
