using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Power
{
    public partial class BaseNetConnectorComponentData
    {
        [DataClassTarget("voltage")]
        private Voltage? _voltage;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _voltage, "voltage", null);
        }
    }
}
