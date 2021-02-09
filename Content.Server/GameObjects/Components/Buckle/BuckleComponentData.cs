using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Buckle
{
    public partial class BuckleComponentData
    {
        [DataClassTarget("delay")]
        public TimeSpan? UnbuckleDelay;

        public void ExposeData(ObjectSerializer serializer)
        {
            float? seconds = 0.25f;
            serializer.DataField(ref seconds, "cooldown", null);

            UnbuckleDelay = seconds != null ? TimeSpan.FromSeconds((float)seconds) : null;
        }
    }
}
