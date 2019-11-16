using System;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    //Default metabolism for reagents. Metabolizes the reagent with no effects
    class DefaultMetabolizable : IMetabolizable
    {
        //Rate of metabolism in units / second
        private int _metabolismRate = 1;
        public int MetabolismRate => _metabolismRate;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
        }

        public int Metabolize(IEntity solutionEntity, string reagentId, float frameTime)
        {
            int metabolismAmount = (int)Math.Round(MetabolismRate * frameTime);
            return metabolismAmount;
        }
    }
}
