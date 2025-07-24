using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Materials.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
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

        // If we've managed to extract something from whatever we're using on the biogenerator, play audio
        if (TryExtractFromProduce(ent, args.Used, args.User) || TryInsertFromStorage(ent, args.Used, args.User))
            _audio.PlayPvs(ent.Comp.ExtractSound, ent);
            args.Handled = true;
    }

    // The old function, crammed into a boolean function to register whether we should play the audio/set the handled args
    private bool TryExtractFromProduce(Entity<ProduceMaterialExtractorComponent> ent, EntityUid used, EntityUid user)
    {
        if (!TryComp<ProduceComponent>(used, out var produce))
            return false;

        if (!_solutionContainer.TryGetSolution(used, produce.SolutionName, out var solution))
            return false;

        // Can produce even have fractional amounts? Does it matter if they do?
        // Questions man was never meant to answer.
        var matAmount = solution.Value.Comp.Solution.Contents
            .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());

        var changed = (int)matAmount;

        if (changed == 0)
        {
            _popup.PopupEntity(Loc.GetString("material-extractor-comp-wrongreagent", ("used", used)), user, user);
            return false;
        }

        _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.ExtractedMaterial, changed);

        QueueDel(used);

        return true;
    }

    private bool TryInsertFromStorage(Entity<ProduceMaterialExtractorComponent> ent, EntityUid used, EntityUid user)
    {
        // If there's no storage component, ollie out of this function
        if (!TryComp<StorageComponent>(used, out var storage))
            return false;
        // We don't need to iterate through all the items if there are no items
        if (storage.StoredItems.Count == 0)
            return false;

        bool hasInserted = false;
        // Run TryExtractFromProduce on every item in storage
        foreach (var (item, _location) in storage.StoredItems)
        {
            if (TryExtractFromProduce(ent, item, user))
                hasInserted = true;
        }

        return hasInserted;
    }
}
