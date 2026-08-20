using Content.Shared.Botany.Components;
using System.Linq;
using JetBrains.Annotations;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles plant analyzer scans and their user interface state.
/// </summary>
public sealed partial class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private PlantSystem _plant = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach || !HasComp<PlantComponent>(target))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.ScanDelay,
            new PlantAnalyzerDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<PlantAnalyzerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(PlantAnalyzerUiKey.Key) || args.Actor != ent.Comp.User)
            return;

        Stop(ent);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target || !HasComp<PlantComponent>(target))
            return;

        _audio.PlayPredicted(ent.Comp.ScanningEndSound, ent.Owner, args.Args.User);

        ent.Comp.Target = target;
        ent.Comp.User = args.Args.User;
        Dirty(ent);

        _toggle.TryActivate(ent.Owner);
        _ui.OpenUi(ent.Owner, PlantAnalyzerUiKey.Key, args.Args.User, true);
        UpdateAnalyzerUi(ent);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<PlantAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            Stop(ent, false);
    }

    [SubscribeLocalEvent]
    private void OnDropped(Entity<PlantAnalyzerComponent> ent, ref DroppedEvent args)
    {
        Stop(ent);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<PlantAnalyzerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        Stop(ent);
    }

    /// <summary>
    /// Updates the analyzer currently observing the specified plant.
    /// </summary>
    [PublicAPI]
    public void UpdatePlantUi(EntityUid plantUid)
    {
        var query = EntityQueryEnumerator<PlantAnalyzerComponent>();
        while (query.MoveNext(out var analyzerUid, out var analyzer))
        {
            if (analyzer.Target != plantUid)
                continue;

            UpdateAnalyzerUi((analyzerUid, analyzer));
        }
    }

    private void UpdateAnalyzerUi(Entity<PlantAnalyzerComponent> ent)
    {
        if (ent.Comp.Target is not { } target || !_ui.IsUiOpen(ent.Owner, PlantAnalyzerUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, PlantAnalyzerUiKey.Key, BuildAnalyzerState(target));
    }

    private void Stop(Entity<PlantAnalyzerComponent> ent, bool deactivate = true)
    {
        if (ent.Comp.Target == null && ent.Comp.User == null)
            return;

        ent.Comp.Target = null;
        ent.Comp.User = null;
        Dirty(ent);
        if (deactivate)
            _toggle.TryDeactivate(ent.Owner);

        _ui.CloseUi(ent.Owner, PlantAnalyzerUiKey.Key);
    }

    private BotanyAnalyzerState BuildAnalyzerState(EntityUid target)
    {
        var state = new BotanyAnalyzerState { Target = GetNetEntity(target) };
        if (Deleted(target))
            return state;

        state.Mutations.AddRange(_plant
            .GetPlantMutationDescriptions(target)
            .Select(mutation => mutation.Id));

        return state;
    }
}
