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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeZoomEvent>(OnZoom);
    }

    private void OnZoom(ContentEyeZoomEvent msg, EntitySessionEventArgs args)
    {
        var ent = args.SenderSession.AttachedEntity;

        if (ent == null || !CanZoom(ent.Value, args.SenderSession))
            return;

        SetZoom(ent.Value, msg.Zoom);
    }

    protected abstract bool CanZoom(EntityUid uid, ICommonSession session);

    private void SetZoom(EntityUid uid, Vector2 zoom)
    {
        if (!TryComp<SharedEyeComponent>(uid, out var eyeComponent))
            return;

        var actual = Vector2.ComponentMax(MinZoom, zoom);
        actual = Vector2.ComponentMin(MaxZoom, zoom);
        eyeComponent.Zoom = actual;
    }

    [Serializable, NetSerializable]
    protected sealed class ContentEyeZoomEvent : EntityEventArgs
    {
        public Vector2 Zoom;
    }
}
