using Content.Client.Examine;
using Content.Shared.Machines.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Machines.EntitySystems;

/// <summary>
/// Client side handling of multipart machines.
/// Handles client side examination events to show the expected layout of the machine
/// based on the origin of the main entity.
/// </summary>
public sealed class MultipartMachineSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string GhostPrototype = "MultipartMachineGhost";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ClientExaminedEvent>(OnMachineExamined);
    }

    /// <summary>
    /// Handles spawning several ghost sprites to show where the different parts of the machine
    /// should go and the rotations they're expected to have.
    /// Depends on the current map location of the origin machine.
    /// </summary>
    /// <param name="ent">Entity/Component that has been inspected</param
    /// <param name="args">Args for the event</param>
    private void OnMachineExamined(Entity<MultipartMachineComponent> ent, ref ClientExaminedEvent args)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        foreach (var part in ent.Comp.Parts.Values)
        {
            if (!part.Entity.HasValue)
            {
                var rotation = ent.Comp.Rotation ?? Angle.Zero;
                var ghostEnt = Spawn(GhostPrototype, new EntityCoordinates(ent.Owner, part.Offset.Rotate(rotation)));
                if (!xformQuery.TryGetComponent(ghostEnt, out var xform))
                    break;

                xform.LocalRotation = part.Rotation + rotation;

                if (part.Sprite != null)
                {
                    var sprite = Comp<SpriteComponent>(ghostEnt);
                    sprite.LayerSetSprite(0, part.Sprite);
                }
            }
        }
    }
}
