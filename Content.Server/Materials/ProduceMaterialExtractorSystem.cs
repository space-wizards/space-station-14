using System.Linq;
using Content.Server.Materials.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Events;
using Robust.Server.Audio;

namespace Content.Server.Materials;

public sealed partial class ProduceMaterialExtractorSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private MaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProduceMaterialExtractorComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ProduceMaterialExtractorComponent, BagDepositAttemptEvent>(OnBagDeposit);
    }

    private void OnInteractUsing(Entity<ProduceMaterialExtractorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.Handled)
            args.Handled = TryInsertProduce(ent, args.Used, args.User);
    }

    private void OnBagDeposit(Entity<ProduceMaterialExtractorComponent> ent, ref BagDepositAttemptEvent args)
    {
        if (!args.Handled)
            args.Handled = TryInsertProduce(ent, args.Item, args.User);
    }

    private bool TryInsertProduce(Entity<ProduceMaterialExtractorComponent> ent, EntityUid item, EntityUid user)
    {
        if (!this.IsPowered(ent, EntityManager))
            return false;

        if (!TryComp<ProduceComponent>(item, out var produce))
            return false;

        if (!_solutionContainer.TryGetSolution(item, produce.TargetSolution, out var solution))
            return false;

        // Can produce even have fractional amounts? Does it matter if they do?
        // Questions man was never meant to answer.
        var matAmount = solution.Value.Comp.Solution.Contents
            .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());

        var changed = (int)matAmount;

        if (changed == 0)
        {
            _popup.PopupEntity(Loc.GetString("material-extractor-comp-wrongreagent", ("used", item)), user, user);
            return false;
        }

        _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.ExtractedMaterial, changed);

        _audio.PlayPvs(ent.Comp.ExtractSound, ent);
        QueueDel(item);
        return true;
    }
}
