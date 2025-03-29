using Content.Client.Examine;
using Content.Shared.Machines.Components;
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
public sealed class MultipartMachineSystem : EntitySystem
{
    private EntProtoId _ghostPrototype = "MultipartMachineGhost";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ClientExaminedEvent>(OnMachineExamined);
        SubscribeLocalEvent<MultipartMachineGhostComponent, TimedDespawnEvent>(OnGhostDespawned);
    }

    /// <summary>
    /// Handles spawning several ghost sprites to show where the different parts of the machine
    /// should go and the rotations they're expected to have.
    /// Can only show one set of ghost parts at a time and their location depends on the current map/grid
    /// location of the origin machine.
    /// </summary>
    /// <param name="ent">Entity/Component that has been inspected</param
    /// <param name="args">Args for the event</param>
    private void OnMachineExamined(Entity<MultipartMachineComponent> ent, ref ClientExaminedEvent args)
    {
        if (ent.Comp.Ghosts.Count != 0)
        {
            // Already showing some part ghosts
            return;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        foreach (var part in ent.Comp.Parts.Values)
        {
            if (!part.Entity.HasValue)
            {
                var rotation = ent.Comp.Rotation ?? Angle.Zero;
                var ghostEnt = Spawn(_ghostPrototype, new EntityCoordinates(ent.Owner, part.Offset.Rotate(rotation)));
                if (!xformQuery.TryGetComponent(ghostEnt, out var xform))
                    break;

                xform.LocalRotation = part.Rotation + rotation;

                Comp<MultipartMachineGhostComponent>(ghostEnt).LinkedMachine = ent;

                if (part.Sprite != null)
                {
                    var sprite = Comp<SpriteComponent>(ghostEnt);
                    sprite.LayerSetSprite(0, part.Sprite);
                }

                ent.Comp.Ghosts.Add(ghostEnt);
            }
        }
    }

    /// <summary>
    /// Handles when a ghost part despawns after its short lifetime.
    /// Will attempt to remove itself from the list of known ghost entities in the main multipart
    /// machine component.
    /// </summary>
    /// <param name="ent">Ghost entity that has been despawned</param>
    /// <param name="args">Args for the event</param>
    private void OnGhostDespawned(Entity<MultipartMachineGhostComponent> ent, ref TimedDespawnEvent args)
    {
        if (!TryComp<MultipartMachineComponent>(ent.Comp.LinkedMachine, out var machine))
            return;

        machine.Ghosts.Remove(ent);
    }
}
