using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds
{
    [Serializable]
    public struct MinMax : IExposeData
    {
        [ViewVariables]
        public int Min;

        [ViewVariables]
        public int Max;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Min, "min", 0);
            serializer.DataField(ref Max, "max", 0);
        }
    }
}
