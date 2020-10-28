using Content.Server.Botany;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class ProduceComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public override string Name => "Produce";

        [ViewVariables]
        public Seed Seed { get; set; } = null;

        public float Potency => Seed.Potency;

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

        public void Grown()
        {
            if (Seed == null)
                return;

            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.LayerSetRSI(0, Seed.PlantRsi);
                sprite.LayerSetState(0, Seed.PlantIconState);
            }

            var solutionContainer = Owner.EnsureComponent<SolutionContainerComponent>();

            solutionContainer.RemoveAllSolution();

            foreach (var (chem, quantity) in Seed.Chemicals)
            {
                var amount = ReagentUnit.New(quantity.Min);
                if(quantity.PotencyDivisor > 0 && Potency > 0)
                    amount += ReagentUnit.New(Potency/quantity.PotencyDivisor);
                amount = ReagentUnit.New((int) MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
                solutionContainer.MaxVolume += amount;
                solutionContainer.Solution.AddReagent(chem, amount);
            }
        }
    }
}
