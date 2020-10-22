using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface IPlantMetabolizable : IExposeData
    {
        /// <summary>
        ///     Metabolize 1 unit of a reagent.
        /// </summary>
        /// <param name="plantHolder"></param>
        /// <param name="reactVolume"></param>
        void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f);
    }
}
