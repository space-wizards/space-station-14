using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.HealthAnalyzer;

public abstract partial class HealthAnalyzerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;
    [Dependency] private EntityQuery<UnrevivableComponent> _unrevivableQuery;

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var analyzer, out var component, out var transform))
        {
            // Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime || component.ScannedEntity is not { } patient)
                continue;

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((analyzer, component));
                continue;
            }

            // Get distance between health analyzer and the scanned entity
            // null is infinite range
            var patientCoordinates = _transformQuery.Comp(patient).Coordinates;
            if (component.MaxScanRange != null && !_transform.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                // Range too far, disable updates until they are back in range
                PauseAnalyzingEntity((analyzer, component));
                DirtyField(analyzer, component, nameof(component.NextUpdate));
                continue;
            }

            component.IsAnalyzerActive = true;
            DirtyFields(analyzer, component, null, nameof(component.IsAnalyzerActive), nameof(component.NextUpdate));
            UpdateUi((analyzer, component));
        }
    }

    [SubscribeLocalEvent]
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> analyzer, ref EntGotInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        StopAnalyzingEntity(analyzer);
    }

    [SubscribeLocalEvent]
    private void OnDropped(Entity<HealthAnalyzerComponent> analyzer, ref DroppedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        StopAnalyzingEntity(analyzer);
    }

    /// <summary>
    /// Trigger the doafter for scanning.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> analyzer, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !_mobStateQuery.HasComp(args.Target.Value))
            return;

        _audio.PlayPredicted(analyzer.Comp.ScanningBeginSound, analyzer, args.User);

        var doAfterCancelled = !_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, analyzer.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), analyzer, target: args.Target, used: analyzer)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || analyzer.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popup.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<HealthAnalyzerComponent> analyzer, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || Deleted(args.Target.Value))
            return;

        if (!analyzer.Comp.Silent)
            _audio.PlayPredicted(analyzer.Comp.ScanningEndSound, analyzer, args.User);

        BeginAnalyzingEntity(analyzer, args.Target.Value);
        OpenUserInterface(args.User, analyzer);
        args.Handled = true;
    }

    private void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> analyzer)
    {
        if (analyzer.Comp.ScannedEntity == null && !analyzer.Comp.IsAnalyzerActive)
            return;

        analyzer.Comp.ScannedEntity = null;
        analyzer.Comp.IsAnalyzerActive = false;
        analyzer.Comp.NextUpdate = TimeSpan.Zero;
        Dirty(analyzer);
        UpdateUi(analyzer);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_ui.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _ui.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it.
    /// </summary>
    /// <param name="analyzer">The health analyzer that should receive the updates.</param>
    /// <param name="target">The entity to start analyzing.</param>
    private void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> analyzer, EntityUid target)
    {
        // Link the health analyzer to the scanned entity
        analyzer.Comp.ScannedEntity = target;
        analyzer.Comp.IsAnalyzerActive = true;
        analyzer.Comp.NextUpdate = _timing.CurTime + analyzer.Comp.UpdateInterval;

        Dirty(analyzer);
        UpdateUi(analyzer);
    }

    /// <summary>
    /// If the scanner is active, sends one last update and sets it to inactive.
    /// </summary>
    /// <param name="analyzer">The health analyzer that's receiving the updates.</param>
    private void PauseAnalyzingEntity(Entity<HealthAnalyzerComponent> analyzer)
    {
        if (!analyzer.Comp.IsAnalyzerActive)
            return;

        analyzer.Comp.IsAnalyzerActive = false;

        DirtyField(analyzer.AsNullable(), nameof(analyzer.Comp.IsAnalyzerActive));
        UpdateUi(analyzer);
    }

    /// <summary>
    /// Creates a HealthAnalyzerState based on the current state of an entity.
    /// </summary>
    /// <param name="target">The entity being scanned.</param>
    /// <param name="scanMode">Whether the analyzer is still scanning.</param>
    /// <returns>Returns a <see cref="HealthAnalyzerUiState"/> without a valid temperature.</returns>
    public virtual HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target, bool scanMode)
    {
        if (!target.HasValue || Deleted(target.Value))
            return new HealthAnalyzerUiState();

        var entity = target.Value;
        var bloodAmount = float.NaN;
        var bleeding = false;
        var unrevivable = false;

        if (_bloodstreamQuery.TryComp(entity, out var bloodstream) &&
            _solutionContainer.ResolveSolution(entity, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out _))
        {
            bloodAmount = _bloodstream.GetBloodLevel(entity);
            bleeding = bloodstream.BleedAmount > 0;
        }

        if (_unrevivableQuery.TryComp(entity, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        return new HealthAnalyzerUiState(
            GetNetEntity(entity),
            float.NaN,
            bloodAmount,
            scanMode,
            bleeding,
            unrevivable
        );
    }

    private void UpdateUi(Entity<HealthAnalyzerComponent> analyzer)
    {
        // Gather all the information the UI needs.
        var state = GetHealthAnalyzerUiState(analyzer.Comp.ScannedEntity, analyzer.Comp.IsAnalyzerActive);
        // Send it to the BUI.
        _ui.SetUiState(analyzer.Owner, HealthAnalyzerUiKey.Key, state);
    }
}
