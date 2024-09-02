using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using LayerChangeOnWeldComponent = Content.Shared.Tools.Components.LayerChangeOnWeldComponent;

namespace Content.Shared.Tools.Systems;

public sealed class WeldableSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    private EntityQuery<WeldableComponent> _query;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WeldableComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<LayerChangeOnWeldComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<WeldableComponent, ExaminedEvent>(OnExamine);

        _query = GetEntityQuery<WeldableComponent>();
    }

    public bool IsWelded(EntityUid uid, WeldableComponent? component = null)
    {
        return _query.Resolve(uid, ref component, false) && component.IsWelded;
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
        if (!_query.Resolve(uid, ref component))
            return false;

        // Other component systems
        var attempt = new WeldableAttemptEvent(user, tool);
        RaiseLocalEvent(uid, attempt);
        if (attempt.Cancelled)
            return false;

        return true;
    }

    private bool TryWeld(EntityUid uid, EntityUid tool, EntityUid user, WeldableComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component))
            return false;

        if (!CanWeld(uid, tool, user, component))
            return false;

        if (!_toolSystem.UseTool(tool, user, uid, component.Time.Seconds, component.WeldingQuality, new WeldFinishedEvent(), component.Fuel))
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

        SetWeldedState(uid, !component.IsWelded, component);

        // Log success
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} {(!component.IsWelded ? "un" : "")}welded {ToPrettyString(uid):target}");
    }

    private void OnWeldChanged(EntityUid uid, LayerChangeOnWeldComponent component, ref WeldableChangedEvent args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            switch (args.IsWelded)
            {
                case true when fixture.CollisionLayer == (int) component.UnWeldedLayer:
                    _physics.SetCollisionLayer(uid, id, fixture, (int) component.WeldedLayer);
                    break;

                case false when fixture.CollisionLayer == (int) component.WeldedLayer:
                    _physics.SetCollisionLayer(uid, id, fixture, (int) component.UnWeldedLayer);
                    break;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, WeldableComponent? component = null)
    {
        if (_query.Resolve(uid, ref component))
            _appearance.SetData(uid, WeldableVisuals.IsWelded, component.IsWelded);
    }

    public void SetWeldedState(EntityUid uid, bool state, WeldableComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component))
            return;

        if (component.IsWelded == state)
            return;

        component.IsWelded = state;
        var ev = new WeldableChangedEvent(component.IsWelded);

        RaiseLocalEvent(uid, ref ev);
        UpdateAppearance(uid, component);
        Dirty(uid, component);
    }

    public void SetWeldingTime(EntityUid uid, TimeSpan time, WeldableComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component))
            return;

        if (component.Time.Equals(time))
            return;

        component.Time = time;
        Dirty(uid, component);
    }
}
