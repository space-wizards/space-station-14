#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Botany;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.PlantMetabolism
{
    public abstract class AdjustAttribute : IPlantMetabolizable
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public float Amount { get; private set; }
        public float Prob { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Amount, "amount", 1f);
            serializer.DataField(this, x => x.Prob, "prob", 1f);
        }

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
