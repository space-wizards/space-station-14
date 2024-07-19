using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Shared.NPC.NuPC;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Systems;

namespace Content.Client.NPC.NuPC;

public sealed class NpcKnowledgeSystem : SharedNpcKnowledgeSystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value || (value && !_admin.IsAdmin()))
                return;

            _enabled = value;
            RaiseNetworkEvent(new RequestNpcKnowledgeEvent()
            {
                Enabled = _enabled,
            });

            if (_enabled)
            {
                _overlays.AddOverlay(new NpcKnowledgeOverlay(EntityManager));
            }
            else
            {
                _knowledge = null;
                _overlays.RemoveOverlay<NpcKnowledgeOverlay>();
            }
        }
    }

    private bool _enabled;

    private NpcKnowledgeDebugEvent? _knowledge;

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("npc_knowledge",
            Loc.GetString("cmd-npc_knowledge-desc"),
            Loc.GetString("cmd-npc_knowledge-help"),
            KnowledgeRequest);

        SubscribeNetworkEvent<NpcKnowledgeDebugEvent>(OnKnowledgeReceived);
    }

    private void OnKnowledgeReceived(NpcKnowledgeDebugEvent ev)
    {
        _knowledge = ev;
    }

    private void KnowledgeRequest(IConsoleShell shell, string argstr, string[] args)
    {
        Enabled = !Enabled;
        shell.WriteLine(Loc.GetString("npc-knowledge-status", ("enabled", Enabled)));
    }

    private sealed class NpcKnowledgeOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public EntityManager EntityManager;

        public DebugPhysicsSystem DebugPhysics;
        public NpcKnowledgeSystem System;
        public SharedPhysicsSystem Physics;
        public SharedTransformSystem Transforms;

        public NpcKnowledgeOverlay(EntityManager entManager)
        {
            EntityManager = entManager;
            DebugPhysics = entManager.System<DebugPhysicsSystem>();
            System = entManager.System<NpcKnowledgeSystem>();
            Physics = entManager.System<SharedPhysicsSystem>();
            Transforms = entManager.System<SharedTransformSystem>();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (System._knowledge == null)
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            foreach (var data in System._knowledge!.Data)
            {
                if (!EntityManager.TryGetEntity(data.Entity, out var uid))
                    continue;

                var transform = Physics.GetPhysicsTransform(uid.Value);

                // Draw sensors
                foreach (var sensor in data.Sensors)
                {
                    // TODO: Move it to physics system.
                    DebugPhysics.DrawShape(args.WorldHandle, sensor.Shape, transform, Color.Aqua.WithAlpha(0.005f));
                    DebugPhysics.DrawShape(args.WorldHandle, sensor.Shape, transform, Color.Aqua.WithAlpha(0.8f), filled: false);
                }

                // Draw stims
                foreach (var mob in data.HostileMobs)
                {
                    if (!EntityManager.TryGetEntity(mob.DebugOwner, out var mobOwner))
                        continue;

                    var worldPos = Transforms.GetWorldPosition(mobOwner.Value);
                    var rect = Box2.CenteredAround(worldPos, new Vector2(0.35f, 0.35f));
                    var adjustedRect = new Box2Rotated(rect, -args.Viewport.Eye!.Rotation, rect.Center);

                    args.WorldHandle.DrawRect(adjustedRect, Color.Firebrick.WithAlpha(0.5f));
                    args.WorldHandle.DrawRect(adjustedRect, Color.Firebrick, filled: false);
                }

                foreach (var mob in data.LastHostileMobPositions)
                {
                    var mapPos = Transforms.ToMapCoordinates(mob.DebugCoordinates);

                    if (mapPos.MapId != args.MapId)
                        continue;

                    var rect = Box2.CenteredAround(mapPos.Position, new Vector2(0.35f, 0.35f));
                    var adjustedRect = new Box2Rotated(rect, -args.Viewport.Eye!.Rotation, rect.Center);

                    args.WorldHandle.DrawRect(adjustedRect, Color.Orange.WithAlpha(0.5f));
                    args.WorldHandle.DrawRect(adjustedRect, Color.Orange, filled: false);
                }
            }
        }
    }
}

