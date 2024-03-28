using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds
{
    [Serializable]
    [DataDefinition]
    public partial struct MinMax
    {
        [DataField("min")]
        public int Min;

        [DataField("max")]
        public int Max;

        public MinMax(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Next(IRobustRandom random)
        {
            return random.Next(Min, Max + 1);
        }
    }
}
