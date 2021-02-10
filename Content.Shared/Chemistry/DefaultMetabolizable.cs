using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    //Default metabolism for reagents. Metabolizes the reagent with no effects
    public class DefaultMetabolizable : IMetabolizable
    {
        //Rate of metabolism in units / second
        private double _metabolismRate = 1;
        public double MetabolismRate => _metabolismRate;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
        }

        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            return ReagentUnit.New(MetabolismRate * tickTime);
        }
    }
}
