using Content.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class PressureProtectionComponent : Component, IPressureProtection
    {
        public override string Name => "PressureProtection";

        [ViewVariables]
        public float HighPressureMultiplier { get; private set; }

        [ViewVariables]
        public float LowPressureMultiplier { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.HighPressureMultiplier, "highPressureMultiplier", 1f);
            serializer.DataField(this, x => x.LowPressureMultiplier, "lowPressureMultiplier", 1f);
        }
    }
}
