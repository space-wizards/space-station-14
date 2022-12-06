using Content.Shared.NPC;
using Content.Shared.NPC.Events;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.NPC;

public sealed class NPCSteeringSystem : SharedNPCSteeringSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public bool DebugEnabled
    {
        get => _debugEnabled;
        set
        {
            if (_debugEnabled == value)
                return;

            _debugEnabled = value;

            if (_debugEnabled)
            {
                _overlay.AddOverlay(new NPCSteeringOverlay(EntityManager));
                RaiseNetworkEvent(new RequestNPCSteeringDebugEvent()
                {
                    Enabled = true
                });
            }
            else
            {
                _overlay.RemoveOverlay<NPCSteeringOverlay>();
                RaiseNetworkEvent(new RequestNPCSteeringDebugEvent()
                {
                    Enabled = false
                });

                foreach (var comp in EntityQuery<NPCSteeringComponent>(true))
                {
                    RemCompDeferred<NPCSteeringComponent>(comp.Owner);
                }
            }
        }
    }

    private bool _debugEnabled;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NPCSteeringDebugEvent>(OnDebugEvent);
        DebugEnabled = true;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        DebugEnabled = false;
    }

    private void OnDebugEvent(NPCSteeringDebugEvent ev)
    {
        if (!DebugEnabled)
            return;

        foreach (var data in ev.Data)
        {
            if (!Exists(data.EntityUid))
                continue;

            var comp = EnsureComp<NPCSteeringComponent>(data.EntityUid);
            comp.Direction = data.Direction;
            comp.DangerMap = data.DangerMap;
            comp.InterestMap = data.InterestMap;
        }
    }
}

public sealed class NPCSteeringOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entManager;

    public NPCSteeringOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (comp, xform) in _entManager.EntityQuery<NPCSteeringComponent, TransformComponent>(true))
        {
            if (xform.MapID != args.MapId)
            {
                continue;
            }

            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            if (!args.WorldAABB.Contains(worldPos))
                continue;

            args.WorldHandle.DrawCircle(worldPos, 1f, Color.Green, false);
            var rotationOffset = worldRot - xform.LocalRotation;

            for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
            {
                var danger = comp.DangerMap[i];
                var angle = Angle.FromDegrees(i * (360 / SharedNPCSteeringSystem.InterestDirections));
                args.WorldHandle.DrawLine(worldPos, worldPos + angle.RotateVec(new Vector2(0f, danger)), Color.Red);
            }

            args.WorldHandle.DrawLine(worldPos, worldPos + rotationOffset.RotateVec(comp.Direction), Color.Blue);
        }
    }
}
