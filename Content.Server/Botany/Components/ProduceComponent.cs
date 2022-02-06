using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

        [DataField("seed")] private string? _seedName;

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

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                sprite.LayerSetRSI(0, Seed.PlantRsi);
                sprite.LayerSetState(0, Seed.PlantIconState);
            }

            var solutionContainer = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner, SolutionName);
            if (solutionContainer == null)
            {
                Logger.Warning($"No solution container found in {nameof(ProduceComponent)}.");
                return;
            }

            solutionContainer.RemoveAllSolution();
            foreach (var (chem, quantity) in Seed.Chemicals)
            {
                var amount = FixedPoint2.New(quantity.Min);
                if (quantity.PotencyDivisor > 0 && Potency > 0)
                    amount += FixedPoint2.New(Potency / quantity.PotencyDivisor);
                amount = FixedPoint2.New((int) MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
                solutionContainer.MaxVolume += amount;
                solutionContainer.AddReagent(chem, amount);
            }
        }
    }
}
