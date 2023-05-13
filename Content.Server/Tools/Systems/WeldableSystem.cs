using Content.Server.Administration.Logs;
using Content.Server.Tools.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Tools.Systems;

public sealed class WeldableSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WeldableComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<LayerChangeOnWeldComponent, WeldableChangedEvent>(OnWeldChanged);
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
        if (!component.Weldable)
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

        if (!_toolSystem.UseTool(tool, user, uid, component.WeldingTime.Seconds, component.WeldingQuality, new WeldFinishedEvent(), fuel: component.FuelConsumption))
            return false;

        // Log attempt
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):user} is {(component.IsWelded ? "un" : "")}welding {ToPrettyString(uid):target} at {Transform(uid).Coordinates:targetlocation}");

        return true;
    }

    private void OnWeldFinished(EntityUid uid, WeldableComponent component, WeldFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        // Check if target is still valid
        if (!CanWeld(uid, args.Used.Value, args.User, component))
            return;

        component.IsWelded = !component.IsWelded;
        RaiseLocalEvent(uid, new WeldableChangedEvent(component.IsWelded), true);

        UpdateAppearance(uid, component);

        // Log success
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} {(!component.IsWelded ? "un" : "")}welded {ToPrettyString(uid):target}");
    }

    private void OnWeldChanged(EntityUid uid, LayerChangeOnWeldComponent component, WeldableChangedEvent args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            switch (args.IsWelded)
            {
                case true when fixture.CollisionLayer == (int) component.UnWeldedLayer:
                    _physics.SetCollisionLayer(uid, fixture, (int) component.WeldedLayer);
                    break;

                case false when fixture.CollisionLayer == (int) component.WeldedLayer:
                    _physics.SetCollisionLayer(uid, fixture, (int) component.UnWeldedLayer);
                    break;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, WeldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;
        _appearance.SetData(uid, WeldableVisuals.IsWelded, component.IsWelded, appearance);
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
