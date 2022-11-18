using Content.Shared.Disease;
using JetBrains.Annotations;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseSatiateThirst : DiseaseEffect
    {
        /// How much thirst is satiated each metabolism tick. Not currently tied to
        /// rate or anything.
        [DataField("factor")]
        public float HydrationFactor { get; set; } = 3.0f;

        /// Satiate thirst if a ThirstComponent can be found
        public override void Effect(DiseaseEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.DiseasedEntity, out ThirstComponent? thirst))
                EntitySystem.Get<ThirstSystem>().UpdateThirst(thirst, HydrationFactor);
        }
    }
}
