#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.Botany.Components;
using Content.Shared.Botany;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class AdjustAttribute : IPlantMetabolizable
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        [DataField("amount")] public float Amount { get; protected set; } = 1;
        [DataField("prob")] public float Prob { get; protected set; } = 1; // = (80);

        /// <summary>
        ///     Checks if the plant holder can metabolize the reagent or not. Checks if it has an alive plant by default.
        /// </summary>
        /// <param name="plantHolder">The entity holding the plant</param>
        /// <param name="plantHolderComponent">The plant holder component</param>
        /// <param name="mustHaveAlivePlant">Whether to check if it has an alive plant or not</param>
        /// <returns></returns>
        public bool CanMetabolize(IEntity plantHolder, [NotNullWhen(true)] out PlantHolderComponent? plantHolderComponent, bool mustHaveAlivePlant = true)
        {
            plantHolderComponent = null;

            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out plantHolderComponent)
                                    || mustHaveAlivePlant && (plantHolderComponent.Seed == null || plantHolderComponent.Dead))
                return false;

            if (Prob >= 1f)
                return true;

            return !(Prob <= 0f) && _robustRandom.Prob(Prob);
        }

        public abstract void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f);
    }
}
