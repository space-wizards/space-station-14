using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets specific sessions scroll and set their zoom directly.
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    private static readonly Vector2 MinZoom = new(0.1f, 0.1f);
    private static readonly Vector2 MaxZoom = new(10f, 10f);
    private static readonly float MinimumChange = 0.1f;

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<ContentEyeZoomEvent>(OnZoom);
        SubscribeLocalEvent<ContentEyeComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ContentEyeComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
    }

    private void OnContentEyeStartup(EntityUid uid, ContentEyeComponent component, ComponentStartup args)
    {
        if (TryComp<SharedEyeComponent>(uid, out var eyeComp))
        {
            component.TargetZoom = eyeComp.Zoom;
            Dirty(component);
        }
    }

    private void OnGetState(EntityUid uid, ContentEyeComponent component, ref ComponentGetState args)
    {
        args.State = new ContentEyeComponentState()
        {
            TargetZoom = component.TargetZoom,
        };
    }

    private void OnHandleState(EntityUid uid, ContentEyeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ContentEyeComponentState state)
            return;

        if (component.TargetZoom.Equals(state.TargetZoom))
            return;

        component.TargetZoom = state.TargetZoom;
        EnsureComp<ActiveContentEyeComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var eyeQuery = GetEntityQuery<SharedEyeComponent>();

        foreach (var (_, comp) in EntityQuery<ActiveContentEyeComponent, ContentEyeComponent>(true))
        {
            // Use a separate query jjuussstt in case any actives mistakenly hang around.
            if (!eyeQuery.TryGetComponent(comp.Owner, out var eyeComp) ||
                eyeComp.Zoom.Equals(comp.TargetZoom))
            {
                RemComp<ActiveContentEyeComponent>(comp.Owner);
                continue;
            }

            var diff = comp.TargetZoom - eyeComp.Zoom;
            var change = diff / 10f;

            if (change.Length < MinimumChange)
                change *= MathF.Min(diff.Length, (MinimumChange / change.Length));

            eyeComp.Zoom += change;
        }
    }

    private void OnZoom(ContentEyeZoomEvent msg, EntitySessionEventArgs args)
    {
        var ent = args.SenderSession.AttachedEntity;
        ContentEyeComponent? component = null;

        if (ent == null || !CanZoom(ent.Value, component))
            return;

        SetZoom(ent.Value, msg.Zoom, component);
    }

    private bool CanZoom(EntityUid uid, ContentEyeComponent? component = null)
    {
        return Resolve(uid, ref component, false);
    }

    private void SetZoom(EntityUid uid, Vector2 zoom, ContentEyeComponent? component = null)
    {
        if (!TryComp<SharedEyeComponent>(uid, out var eyeComponent) ||
            eyeComponent.Zoom.Equals(zoom) ||
            !Resolve(uid, ref component, false) ||
            component.TargetZoom.Equals(zoom))
        {
            return;
        }

        var actual = Vector2.ComponentMax(MinZoom, zoom);
        actual = Vector2.ComponentMin(MaxZoom, zoom);
        component.TargetZoom = actual;
        EnsureComp<ActiveContentEyeComponent>(uid);
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    [Serializable, NetSerializable]
    protected sealed class ContentEyeZoomEvent : EntityEventArgs
    {
        public Vector2 Zoom;
    }

    [Serializable, NetSerializable]
    private sealed class ContentEyeComponentState : ComponentState
    {
        public Vector2 TargetZoom;
    }
}
