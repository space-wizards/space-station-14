using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Power
{
    public partial class BaseNetConnectorComponentData
    {
        [CustomYamlField("voltage")]
        private Voltage? _voltage;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _voltage, "voltage", null);
        }
    }
}
