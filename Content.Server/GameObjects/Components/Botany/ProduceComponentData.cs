#nullable enable
using Content.Server.Botany;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Botany
{
    public partial class ProduceComponentData
    {
        [DataClassTarget("Seed")] public Seed? Seed;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            serializer.DataReadFunction<string?>("seed", null,
                (s) =>
                {
                    if(!string.IsNullOrEmpty(s))
                        Seed = prototypeManager.Index<Seed>(s);
                });
        }
    }
}
