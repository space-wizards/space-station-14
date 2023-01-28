using Content.Server.Administration.Logs;
using Content.Server.Tools.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;

namespace Content.Server.Tools.Systems;

public sealed class WeldableSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WeldableComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<WeldableComponent, WeldCancelledEvent>(OnWeldCanceled);
        SubscribeLocalEvent<WeldableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, WeldableComponent component, ExaminedEvent args)
    {
        if (component.IsWelded && component.WeldedExamineMessage != null)
            args.PushText(Loc.GetString(component.WeldedExamineMessage));
    }

    private void OnInteractUsing(EntityUid uid, WeldableComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryWeld(uid, args.Used, args.User, component);
    }

    private bool CanWeld(EntityUid uid, EntityUid tool, EntityUid user, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // Basic checks
        if (!component.Weldable || component.BeingWelded)
            return false;
        if (!_toolSystem.HasQuality(tool, component.WeldingQuality))
            return false;

        // Other component systems
        var attempt = new WeldableAttemptEvent(user, tool);
        RaiseLocalEvent(uid, attempt, true);
        if (attempt.Cancelled)
            return false;

        return true;
    }

    private bool TryWeld(EntityUid uid, EntityUid tool, EntityUid user, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanWeld(uid, tool, user, component))
            return false;

        component.BeingWelded = _toolSystem.UseTool(tool, user, uid, component.FuelConsumption,
            component.WeldingTime.Seconds, component.WeldingQuality,
            new WeldFinishedEvent(user, tool), new WeldCancelledEvent(), uid);

        // Log attempt
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):user} is {(component.IsWelded ? "un" : "")}welding {ToPrettyString(uid):target} at {Transform(uid).Coordinates:targetlocation}");

        return true;
    }

    private void OnWeldFinished(EntityUid uid, WeldableComponent component, WeldFinishedEvent args)
    {
        component.BeingWelded = false;

        // Check if target is still valid
        if (!CanWeld(uid, args.Tool, args.User, component))
            return;

        component.IsWelded = !component.IsWelded;
        RaiseLocalEvent(uid, new WeldableChangedEvent(component.IsWelded), true);

        UpdateAppearance(uid, component);

        // Log success
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} {(!component.IsWelded ? "un" : "")}welded {ToPrettyString(uid):target}");
    }

    private void OnWeldCanceled(EntityUid uid, WeldableComponent component, WeldCancelledEvent args)
    {
        component.BeingWelded = false;
    }

    private void UpdateAppearance(EntityUid uid, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;
        appearance.SetData(WeldableVisuals.IsWelded, component.IsWelded);
    }

    public void ForceWeldedState(EntityUid uid, bool state, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.IsWelded = state;

        RaiseLocalEvent(uid, new WeldableChangedEvent(component.IsWelded));

        UpdateAppearance(uid, component);
    }

    public void SetWeldingTime(EntityUid uid, TimeSpan time, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.WeldingTime = time;
    }

    /// <summary>
    ///     Raised after welding do_after has finished. It doesn't guarantee success,
    ///     use <see cref="WeldableChangedEvent"/> to get updated status.
    /// </summary>
    private sealed class WeldFinishedEvent : EntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Tool;

        public WeldFinishedEvent(EntityUid user, EntityUid tool)
        {
            User = user;
            Tool = tool;
        }
    }

    /// <summary>
    ///     Raised when entity welding has failed.
    /// </summary>
    private sealed class WeldCancelledEvent : EntityEventArgs
    {

    }
}

/// <summary>
///     Checks that entity can be weld/unweld.
///     Raised twice: before do_after and after to check that entity still valid.
/// </summary>
public sealed class WeldableAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Tool;

    public WeldableAttemptEvent(EntityUid user, EntityUid tool)
    {
        User = user;
        Tool = tool;
    }
}

/// <summary>
///     Raised when <see cref="WeldableComponent.IsWelded"/> has changed.
/// </summary>
public sealed class WeldableChangedEvent : EntityEventArgs
{
    public readonly bool IsWelded;

    public WeldableChangedEvent(bool isWelded)
    {
        IsWelded = isWelded;
    }
}
