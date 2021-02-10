using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface IPlantMetabolizable : IExposeData
    {
        /// <summary>
        ///     Metabolize <paramref name="customPlantMetabolism"/> unit(s) of a reagent.
        /// </summary>
        /// <param name="plantHolder">Entity holding the plant</param>
        /// <param name="customPlantMetabolism">Units to metabolize</param>
        void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f);
    }
}
