using Content.Server.Botany;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class SeedComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Seed";
        [ViewVariables]
        public Seed Seed { get; set; } = null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadFunction<string>("seed", null,
                (s) =>
                {
                    if(!string.IsNullOrEmpty(s))
                        Seed = _prototypeManager.Index<Seed>(s);
                });
        }
    }
}
