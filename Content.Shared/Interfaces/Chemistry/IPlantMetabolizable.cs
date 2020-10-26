using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

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
