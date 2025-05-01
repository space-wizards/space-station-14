using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Materials.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Robust.Server.Audio;

namespace Content.Server.Materials;

public sealed class ProduceMaterialExtractorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProduceMaterialExtractorComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<ProduceMaterialExtractorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!this.IsPowered(ent, EntityManager))
            return;

        if (!TryComp<ProduceComponent>(args.Used, out var produce))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, produce.SolutionName, out var solution))
            return;

        // Can produce even have fractional amounts? Does it matter if they do?
        // Questions man was never meant to answer.
        var matAmount = solution.Value.Comp.Solution.Contents
            .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());
        _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.ExtractedMaterial, (int) matAmount);

        _audio.PlayPvs(ent.Comp.ExtractSound, ent);
        QueueDel(args.Used);
        args.Handled = true;
    }
}
