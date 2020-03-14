using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    //Default metabolism for reagents. Metabolizes the reagent with no effects
    class DefaultMetabolizable : IMetabolizable
    {
#pragma warning disable 649
        [Dependency] private readonly IRounderForReagents _rounder;
#pragma warning restore 649
        //Rate of metabolism in units / second
        private decimal _metabolismRate = 1;
        public decimal MetabolismRate => _metabolismRate;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
        }

        decimal IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = _rounder.Round(MetabolismRate * (decimal)tickTime);
            return metabolismAmount;
        }
    }
}
