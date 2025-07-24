using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Materials.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;

namespace Content.Server.Materials;

public sealed class ProduceMaterialExtractorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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

        var changed = (int)matAmount;

        if (changed == 0)
        {
            _popup.PopupEntity(Loc.GetString("material-extractor-comp-wrongreagent", ("used", args.Used)), args.User, args.User);
            return;
        }

        _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.ExtractedMaterial, changed);

        _audio.PlayPvs(ent.Comp.ExtractSound, ent);
        QueueDel(args.Used);
        args.Handled = true;
    }
}
