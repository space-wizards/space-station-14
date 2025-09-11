using Content.Client.Examine;
using Content.Client.Machines.Components;
using Content.Shared.Machines.Components;
using Content.Shared.Machines.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;

namespace Content.Client.Machines.EntitySystems;

/// <summary>
/// Client side handling of multipart machines.
/// Handles client side examination events to show the expected layout of the machine
/// based on the origin of the main entity.
/// </summary>
public sealed class MultipartMachineSystem : SharedMultipartMachineSystem
{
    private readonly EntProtoId _ghostPrototype = "MultipartMachineGhost";
    private readonly Color _partiallyTransparent = new Color(255, 255, 255, 180);

    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization= default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ClientExaminedEvent>(OnMachineExamined);
        SubscribeLocalEvent<MultipartMachineComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<MultipartMachineGhostComponent, TimedDespawnEvent>(OnGhostDespawned);
    }

    /// <summary>
    /// Handles spawning several ghost sprites to show where the different parts of the machine
    /// should go and the rotations they're expected to have.
    /// Can only show one set of ghost parts at a time and their location depends on the current map/grid
    /// location of the origin machine.
    /// </summary>
    /// <param name="ent">Entity/Component that has been inspected.</param>
    /// <param name="args">Args for the event.</param>
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

            var entityCoords = new EntityCoordinates(ent.Owner, part.Offset);
            var ghostEnt = Spawn(_ghostPrototype, entityCoords);

            if (!XformQuery.TryGetComponent(ghostEnt, out var xform))
                break;

            xform.LocalRotation = part.Rotation;

            Comp<MultipartMachineGhostComponent>(ghostEnt).LinkedMachine = ent;

            ent.Comp.Ghosts.Add(ghostEnt);

            if (part.GhostProto == null)
                continue;

            var entProto = _prototype.Index(part.GhostProto.Value);
            if (!entProto.Components.TryGetComponent("Sprite", out var s) || s is not SpriteComponent protoSprite)
                return;

            var ghostSprite = EnsureComp<SpriteComponent>(ghostEnt);
            _serialization.CopyTo(protoSprite, ref ghostSprite, notNullableOverride: true);

            _sprite.SetColor((ghostEnt, ghostSprite), _partiallyTransparent);

            _metaData.SetEntityName(ghostEnt, entProto.Name);
            _metaData.SetEntityDescription(ghostEnt, entProto.Description);
        }
    }

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
    private void OnGhostDespawned(Entity<MultipartMachineGhostComponent> ent, ref TimedDespawnEvent args)
    {
        if (!TryComp<MultipartMachineComponent>(ent.Comp.LinkedMachine, out var machine))
            return;

        machine.Ghosts.Remove(ent);
    }
}
