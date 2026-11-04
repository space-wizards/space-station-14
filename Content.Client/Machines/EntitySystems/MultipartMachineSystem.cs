using Content.Client.Examine;
using Content.Client.Machines.Components;
using Content.Shared.Machines.Components;
using Content.Shared.Machines.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Client.Machines.EntitySystems;

/// <summary>
/// Client side handling of multipart machines.
/// Handles client side examination events to show the expected layout of the machine
/// based on the origin of the main entity.
/// </summary>
public sealed partial class MultipartMachineSystem : SharedMultipartMachineSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<MultipartMachineGhostComponent> _ghostQuery;
    [Dependency] private EntityQuery<MultipartMachineComponent> _machineQuery;

    private static readonly EntProtoId GhostPrototype = "MultipartMachineGhost";

    /// <summary>
    /// Handles spawning several ghost sprites to show where the different parts of the machine
    /// should go and the rotations they're expected to have.
    /// Can only show one set of ghost parts at a time and their location depends on the current map/grid
    /// location of the origin machine.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMachineExamined(Entity<MultipartMachineComponent> ent, ref ClientExaminedEvent args)
    {
        if (ent.Comp.Ghosts.Count != 0)
        {
            // Already showing some part ghosts
            return;
        }

        foreach (var part in ent.Comp.Parts.Values)
        {
            if (part.Entity.HasValue)
                continue;

            if (part.GhostProto == null)
                continue;

            var entityCoords = new EntityCoordinates(ent.Owner, part.Offset);
            var ghostEnt = SpawnAttachedTo(GhostPrototype, entityCoords, rotation: part.Rotation);
            var protoEnt = Spawn(part.GhostProto.Value);

            if (_ghostQuery.TryComp(ghostEnt, out var ghostComp))
                ghostComp.LinkedMachine = ent;

            ent.Comp.Ghosts.Add(ghostEnt);

            var entProto = ProtoMan.Index(part.GhostProto.Value);

            if (!_spriteQuery.TryComp(ghostEnt, out var ghostSprite)
                || !_spriteQuery.TryComp(protoEnt, out var protoSprite))
                continue;

            _sprite.CopySprite((protoEnt, protoSprite), (ghostEnt, ghostSprite));
            _sprite.SetColor((ghostEnt, ghostSprite), protoSprite.Color.WithAlpha(0.7f));

            _metaData.SetEntityName(ghostEnt, entProto.Name);
            _metaData.SetEntityDescription(ghostEnt, entProto.Description);
            QueueDel(protoEnt);
        }
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<MultipartMachineComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        foreach (var part in ent.Comp.Parts.Values)
        {
            part.Entity = part.NetEntity.HasValue ? EnsureEntity<MultipartMachinePartComponent>(part.NetEntity.Value, ent) : null;
        }
    }

    /// <summary>
    /// Handles when a ghost part despawns after its short lifetime.
    /// Will attempt to remove itself from the list of known ghost entities in the main multipart
    /// machine component.
    /// </summary>
    /// <param name="ent">Ghost entity that has been despawned.</param>
    /// <param name="args">Args for the event.</param>
    [SubscribeLocalEvent]
    private void OnGhostDespawned(Entity<MultipartMachineGhostComponent> ent, ref TimedDespawnEvent args)
    {
        if (!_machineQuery.TryComp(ent.Comp.LinkedMachine, out var machine))
            return;

        machine.Ghosts.Remove(ent);
    }
}
