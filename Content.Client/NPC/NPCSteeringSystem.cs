using System.Numerics;
using Content.Client.Physics.Controllers;
using Content.Client.PhysicsSystem.Controllers;
using Content.Shared.Movement.Components;
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

                var query = AllEntityQuery<NPCSteeringComponent>();
                while (query.MoveNext(out var uid, out var npc))
                {
                    RemCompDeferred<NPCSteeringComponent>(uid);
                }
            }
        }
    }

    private bool _debugEnabled;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NPCSteeringDebugEvent>(OnDebugEvent);
    }

    private void OnDebugEvent(NPCSteeringDebugEvent ev)
    {
        if (!DebugEnabled)
            return;

        foreach (var data in ev.Data)
        {
            var entity = GetEntity(data.EntityUid);

            if (!Exists(entity))
                continue;

            var comp = EnsureComp<NPCSteeringComponent>(entity);
            comp.Direction = data.Direction;
            comp.DangerMap = data.Danger;
            comp.InterestMap = data.Interest;
            comp.DangerPoints = data.DangerPoints;
        }
    }
}

public sealed class NPCSteeringOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transformSystem;

    public NPCSteeringOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        _transformSystem = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (comp, mover, xform) in _entManager.EntityQuery<NPCSteeringComponent, InputMoverComponent, TransformComponent>(true))
        {
            if (xform.MapID != args.MapId)
            {
                continue;
            }

            var (worldPos, worldRot) = _transformSystem.GetWorldPositionRotation(xform);

            if (!args.WorldAABB.Contains(worldPos))
                continue;

            args.WorldHandle.DrawCircle(worldPos, 1f, Color.Green, false);
            var rotationOffset = _entManager.System<MoverController>().GetParentGridAngle(mover);

            foreach (var point in comp.DangerPoints)
            {
                args.WorldHandle.DrawCircle(point, 0.1f, Color.Red.WithAlpha(0.6f));
            }

            for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
            {
                var danger = comp.DangerMap[i];
                var interest = comp.InterestMap[i];
                var angle = Angle.FromDegrees(i * (360 / SharedNPCSteeringSystem.InterestDirections));
                args.WorldHandle.DrawLine(worldPos, worldPos + (rotationOffset + angle).RotateVec(new Vector2(interest, 0f)), Color.LimeGreen);
                args.WorldHandle.DrawLine(worldPos, worldPos + (rotationOffset + angle).RotateVec(new Vector2(danger, 0f)), Color.Red);
            }

            args.WorldHandle.DrawLine(worldPos, worldPos + rotationOffset.RotateVec(comp.Direction), Color.Cyan);
        }
    }
}
