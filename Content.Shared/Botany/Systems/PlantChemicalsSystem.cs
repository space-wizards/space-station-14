using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared.Botany.Systems;

public sealed class PlantChemicalsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantChemicalsComponent, ProduceCreatedEvent>(OnProduceCreated);
    }

    // TODO: mutation

    private void OnProduceCreated(Entity<PlantChemicalsComponent> ent, ref ProduceCreatedEvent args)
    {
        var uid = args.Produce;

        var solution = _solution.EnsureSolution(uid, ent.Comp.Solution, FixedPoint2.Zero, out _);
        solution.RemoveAllSolution();

        var potency = ent.Comp.Potency;
        foreach (var (id, chem) in ent.Comp.Chemicals)
        {
            var quantity = Math.Min(chem.Min + potency / chem.PotencyDivisor, chem.Max);
            var amount = FixedPoint2.New(chem.Min);
            amount += potency / chem.PotencyDivisor;
            if (amount > chem.Max)
                amount = chem.Max;

            solution.MaxVolume += amount;
            solution.AddReagent(id, amount);
        }

        // high produce plants are bigger
        _appearance.SetData(uid, ProduceVisuals.Potency, potency);
    }
}
