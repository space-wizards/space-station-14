#nullable enable
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public class ProduceComponent : Component, ISerializationHooks
    {
        public override string Name => "Produce";

        [DataField("seed")]
        private string? _seedName;

        [ViewVariables]
        public Seed? Seed
        {
            get => _seedName != null ? IoCManager.Resolve<IPrototypeManager>().Index<Seed>(_seedName) : null;
            set => _seedName = value?.ID;
        }

        public float Potency => Seed?.Potency ?? 0;

        public void Grown()
        {
            if (Seed == null)
                return;

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
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
