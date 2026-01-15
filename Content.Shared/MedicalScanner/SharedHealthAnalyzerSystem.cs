using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.MedicalScanner;

public abstract class SharedHealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<HealthAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var analyzer, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not {} patient
                || component.ScannerUser is null)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((analyzer, component));
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between health analyzer and the scanned entity
            //null is infinite range
            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value)
                // In case their UI is closed, also disable updates.
                || !_uiSystem.IsUiOpen(analyzer, HealthAnalyzerUiKey.Key))
            {
                //Range too far, disable updates
                StopAnalyzingEntity((analyzer, component));
                continue;
            }

            UpdateScannedUser((analyzer, component), component.ScannerUser.Value, patient, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> analyzer, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(analyzer.Owner, user: args.User))
            return;

        _audio.PlayPvs(analyzer.Comp.ScanningBeginSound, analyzer);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, analyzer.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), analyzer, target: args.Target, used: analyzer)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || analyzer.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HealthAnalyzerComponent> analyzer, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(analyzer.Owner, user: args.User))
            return;

        if (!analyzer.Comp.Silent)
            _audio.PlayPredicted(analyzer.Comp.ScanningEndSound, analyzer, args.User);

        OpenUserInterface(args.User, analyzer);
        BeginAnalyzingEntity(analyzer, args.User, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is not null)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<HealthAnalyzerComponent> analyzer, ref ItemToggledEvent args)
    {
        if (!args.Activated && analyzer.Comp.ScannedEntity is { } patient && args.User != null)
            StopAnalyzingEntity(analyzer);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<HealthAnalyzerComponent> analyzer, ref DroppedEvent args)
    {
        if (analyzer.Comp.ScannedEntity is not null)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="analyzer">The health analyzer that should receive the updates</param>
    /// <param name="user">The user who is using the health analyzer.</param>
    /// <param name="target">The entity to start analyzing</param>
    public void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> analyzer, EntityUid user, EntityUid target)
    {
        //Link the health analyzer to the scanned entity
        analyzer.Comp.ScannedEntity = target;
        analyzer.Comp.ScannerUser = user;

        _toggle.TryActivate(analyzer.Owner);

        UpdateScannedUser(analyzer, user, target, true);
        Dirty(analyzer);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="user">The user who is using the health analyzer</param>
    /// <param name="target">The entity to analyze</param>
    public void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer)
    {
        if (healthAnalyzer.Comp.ScannedEntity is not { } patient
            || healthAnalyzer.Comp.ScannerUser is not { } user)
            return;

        UpdateScannedUser(healthAnalyzer, user, patient, false);

        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;
        healthAnalyzer.Comp.ScannerUser = null;

        _toggle.TryDeactivate(healthAnalyzer.Owner);
        Dirty(healthAnalyzer);
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer</param>
    /// <param name="user">The user who is using the health analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    private void UpdateScannedUser(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid user, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(healthAnalyzer, HealthAnalyzerUiKey.Key))
            return;

        if (!HasComp<DamageableComponent>(target))
            return;

        var bloodAmount = float.NaN;
        var bleeding = false;
        var unrevivable = false;

        if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(target, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out _))
        {
            bloodAmount = _bloodstreamSystem.GetBloodLevel(target);
            bleeding = bloodstream.BleedAmount > 0;
        }

        if (TryComp<UnrevivableComponent>(target, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        var message = new HealthAnalyzerScannedUserMessage(
            GetNetEntity(target),
            bloodAmount,
            scanMode,
            bleeding,
            unrevivable,
            user);

        UpdateUi(healthAnalyzer, message);
    }

    protected virtual void UpdateUi(Entity<HealthAnalyzerComponent> analyzer, HealthAnalyzerScannedUserMessage message)
    {

    }
}
