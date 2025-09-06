using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.AbstractAnalyzer;

public abstract class AbstractAnalyzerSystem<TAnalyzerComponent, TAnalyzerDoAfterEvent> : EntitySystem
    where TAnalyzerComponent : AbstractAnalyzerComponent
    where TAnalyzerDoAfterEvent : SimpleDoAfterEvent, new()
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TAnalyzerComponent, TAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<TAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<TAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<TAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<TAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not { } target)
                continue;

            if (Deleted(target))
            {
                StopAnalyzingEntity((uid, component), target);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between analyzer and the scanned entity
            var targetCoordinates = Transform(target).Coordinates;
            if (component.MaxScanRange is { } maxScanRange && !_transformSystem.InRange(targetCoordinates, transform.Coordinates, maxScanRange))
            {
                //Range too far, disable updates
                StopAnalyzingEntity((uid, component), target);
                continue;
            }

            UpdateScannedUser(uid, target, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<TAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !ValidScanTarget(args.Target) || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        _audio.PlayPredicted(uid.Comp.ScanningBeginSound, uid, null);

        var doAfterEvent = _typeFactory.CreateInstance<TAnalyzerDoAfterEvent>();
        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, doAfterEvent, uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent || args.Target is null)
            return;

        if (ScanTargetPopupMessage(uid, args, out var msg))
            _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<TAnalyzerComponent> uid, ref TAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPredicted(uid.Comp.ScanningEndSound, uid, null);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<TAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { })
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<TAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } target)
            StopAnalyzingEntity(ent, target);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<TAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { })
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, GetUiKey()))
            return;

        _uiSystem.OpenUi(analyzer, GetUiKey(), user);
    }

    /// <summary>
    /// Mark the entity as being analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="analyzer">The analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    private void BeginAnalyzingEntity(Entity<TAnalyzerComponent> analyzer, EntityUid target)
    {
        //Link the analyzer to the scanned entity
        analyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(analyzer.Owner);

        UpdateScannedUser(analyzer, target, true);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="analyzer">The analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<TAnalyzerComponent> analyzer, EntityUid target)
    {
        //Unlink the analyzer
        analyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(analyzer.Owner);

        UpdateScannedUser(analyzer, target, false);
    }

    /// <summary>
    /// Send an update for the target to the analyzer
    /// </summary>
    /// <param name="analyzer">The analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    public abstract void UpdateScannedUser(EntityUid analyzer, EntityUid target, bool scanMode);

    /// <returns>A <see cref="Robust.Shared.Serialization.NetSerializableAttribute"/> byte enum key.</returns>
    protected abstract Enum GetUiKey();

    /// <summary>
    /// The message the scan target recieves on scan.
    /// </summary>
    /// <returns>true if the message should be shown</returns>
    protected abstract bool ScanTargetPopupMessage(Entity<TAnalyzerComponent> uid, AfterInteractEvent args, [NotNullWhen(true)] out string? message);

    /// <summary>
    /// Used to validate if a specific entity is a valid target for a specific analyzer.
    /// </summary>
    protected abstract bool ValidScanTarget(EntityUid? target);
}
