using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Buckle
{
    public partial class BuckleComponentData : ISerializationHooks
    {
        [DataClassTarget("delay")]
        public TimeSpan? UnbuckleDelay;

        [DataField("cooldown")] private float? seconds = 0.25f;

        public void AfterDeserialization()
        {
            UnbuckleDelay = seconds != null ? TimeSpan.FromSeconds((float)seconds) : null;
        }

        public void BeforeSerialization()
        {
            seconds = UnbuckleDelay?.Seconds;
        }
    }
}
