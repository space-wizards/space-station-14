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

        public bool CanMetabolize()
        {
            if (Prob >= 1f)
                return true;

            if (Prob <= 0f)
                return false;

            return _robustRandom.Prob(Prob);
        }

        public abstract void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f);
    }
}
