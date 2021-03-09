#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface IPlantMetabolizable
    {
        /// <summary>
        ///     Metabolize <paramref name="customPlantMetabolism"/> unit(s) of a reagent.
        /// </summary>
        /// <param name="plantHolder">Entity holding the plant</param>
        /// <param name="customPlantMetabolism">Units to metabolize</param>
        void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f);
    }
}
