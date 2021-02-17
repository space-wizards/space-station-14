#nullable enable
using Content.Server.Botany;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Botany
{
    public partial class ProduceComponentData : ISerializationHooks
    {
        [DataField("seed")]
        private string? _seedName;

        [DataClassTarget("Seed")] public Seed? Seed;

        public void AfterDeserialization()
        {
            if (_seedName != null)
            {
                Seed = IoCManager.Resolve<IPrototypeManager>().Index<Seed>(_seedName);
            }
        }
    }
}
