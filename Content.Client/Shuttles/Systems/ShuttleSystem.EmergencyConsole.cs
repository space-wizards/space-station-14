using System.Numerics;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Shuttles.Systems;

public sealed partial class ShuttleSystem : SharedShuttleSystem
{
    /// <summary>
    /// Should we show the expected emergency shuttle position.
    /// </summary>
    public bool EnableShuttlePosition
    {
        get => _enableShuttlePosition;
        set
        {
            if (_enableShuttlePosition == value) return;

            _enableShuttlePosition = value;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();

            if (_enableShuttlePosition)
            {
                _overlay = new EmergencyShuttleOverlay(EntityManager.TransformQuery, XformSystem);
                overlayManager.AddOverlay(_overlay);
                RaiseNetworkEvent(new EmergencyShuttleRequestPositionMessage());
            }
            else
            {
                overlayManager.RemoveOverlay(_overlay!);
                _overlay = null;
            }
        }
    }

    private bool _enableShuttlePosition;
    private EmergencyShuttleOverlay? _overlay;

    private void InitializeEmergency()
    {
        SubscribeNetworkEvent<EmergencyShuttlePositionMessage>(OnShuttlePosMessage);
    }

    private void OnShuttlePosMessage(EmergencyShuttlePositionMessage ev)
    {
        if (_overlay == null) return;

        _overlay.StationUid = GetEntity(ev.StationUid);
        _overlay.Position = ev.Position;
    }
}

/// <summary>
/// Shows the expected position of the emergency shuttle. Nothing more.
/// </summary>
public sealed class EmergencyShuttleOverlay : Overlay
{
    private readonly EntityQuery<TransformComponent> _transformQuery;
    private readonly SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public EntityUid? StationUid;
    public Box2? Position;

    public EmergencyShuttleOverlay(EntityQuery<TransformComponent> transformQuery, SharedTransformSystem transformSystem)
    {
        _transformQuery = transformQuery;
        _transformSystem = transformSystem;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Position == null || !_transformQuery.TryGetComponent(StationUid, out var xform))
            return;

        args.WorldHandle.SetTransform(_transformSystem.GetWorldMatrix(xform));
        args.WorldHandle.DrawRect(Position.Value, Color.Red.WithAlpha(100));
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
