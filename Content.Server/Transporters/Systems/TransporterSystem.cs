using System.Threading;
using Content.Server.NPC;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Transporters.System;

public sealed partial class TransporterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public EntityUid? GetTarget(Entity<TransporterComponent> uid)
    {
        return uid.Comp.Target;
    }
}
