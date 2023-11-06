using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.ParticleAccelerator.EntitySystems;

[UsedImplicitly]
public sealed partial class ParticleAcceleratorSystem
{
    private void InitializePartSystem()
    {
        SubscribeLocalEvent<ParticleAcceleratorPartComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ParticleAcceleratorPartComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<ParticleAcceleratorPartComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
    }

    public void RescanParts(EntityUid uid, ICommonSession? user = null, ParticleAcceleratorControlBoxComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        if (controller.CurrentlyRescanning)
            return;

        SwitchOff(uid, user, controller);

        var partQuery = GetEntityQuery<ParticleAcceleratorPartComponent>();
        foreach (var part in AllParts(uid, controller))
        {
            if (partQuery.TryGetComponent(part, out var partState))
                partState.Master = null;
        }

        controller.Assembled = false;
        controller.FuelChamber = null;
        controller.EndCap = null;
        controller.PowerBox = null;
        controller.PortEmitter = null;
        controller.ForeEmitter = null;
        controller.StarboardEmitter = null;

        var xformQuery = GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(uid, out var xform) || !xform.Anchored)
            return;

        var gridUid = xform.GridUid;
        if (gridUid == null || gridUid != xform.ParentUid || !_mapManager.TryGetGrid(gridUid, out var grid))
            return;

        // Find fuel chamber first by scanning cardinals.
        var fuelQuery = GetEntityQuery<ParticleAcceleratorFuelChamberComponent>();
        foreach (var adjacent in grid.GetCardinalNeighborCells(xform.Coordinates))
        {
            if (fuelQuery.HasComponent(adjacent)
            && partQuery.TryGetComponent(adjacent, out var partState)
            && partState.Master == null)
            {
                controller.FuelChamber = adjacent;
                break;
            }
        }

        if (controller.FuelChamber == null)
        {
            UpdateUI(uid, controller);
            return;
        }

        // When we call SetLocalRotation down there to rotate the control box,
        // that ends up re-entrantly calling RescanParts() through the move event.
        // You'll have to take my word for it that that breaks everything, yeah?
        controller.CurrentlyRescanning = true;

        // Align ourselves to match fuel chamber orientation.
        // This means that if you mess up the orientation of the control box it's not a big deal,
        // because the sprite is far from obvious about the orientation.
        var fuelXform = xformQuery.GetComponent(controller.FuelChamber!.Value);
        var rotation = fuelXform.LocalRotation;
        _transformSystem.SetLocalRotation(uid, rotation, xform);

        // Calculate offsets for each of the parts of the PA.
        // These are all done relative to the fuel chamber BC that is basically the center of the machine.
        var positionFuelChamber = grid.TileIndicesFor(fuelXform.Coordinates);                           //       //
        var positionEndCap = positionFuelChamber + (Vector2i) rotation.RotateVec(new Vector2(0, 1));               //   n   // n: End Cap
        var positionPowerBox = positionFuelChamber + (Vector2i) rotation.RotateVec(new Vector2(0, -1));            //  CF   // C: Control Box, F: Fuel Chamber
        var positionPortEmitter = positionFuelChamber + (Vector2i) rotation.RotateVec(new Vector2(1, -2));         //   P   // P: Power Box
        var positionForeEmitter = positionFuelChamber + (Vector2i) rotation.RotateVec(new Vector2(0, -2));         //  EEE  // E: Emitter (Starboard, Fore, Port)
        var positionStarboardEmitter = positionFuelChamber + (Vector2i) rotation.RotateVec(new Vector2(-1, -2));   //       //

        ScanPart<ParticleAcceleratorEndCapComponent>(gridUid!.Value, positionEndCap, rotation, out controller.EndCap, out var _, grid);
        ScanPart<ParticleAcceleratorPowerBoxComponent>(gridUid!.Value, positionPowerBox, rotation, out controller.PowerBox, out var _, grid);

        if (!ScanPart<ParticleAcceleratorEmitterComponent>(gridUid!.Value, positionPortEmitter, rotation, out controller.PortEmitter, out var portEmitter, grid)
        || portEmitter!.Type != ParticleAcceleratorEmitterType.Port)
            controller.PortEmitter = null;

        if (!ScanPart<ParticleAcceleratorEmitterComponent>(gridUid!.Value, positionForeEmitter, rotation, out controller.ForeEmitter, out var foreEmitter, grid)
        || foreEmitter!.Type != ParticleAcceleratorEmitterType.Fore)
            controller.ForeEmitter = null;

        if (!ScanPart<ParticleAcceleratorEmitterComponent>(gridUid!.Value, positionStarboardEmitter, rotation, out controller.StarboardEmitter, out var starboardEmitter, grid)
        || starboardEmitter!.Type != ParticleAcceleratorEmitterType.Starboard)
            controller.StarboardEmitter = null;

        controller.Assembled =
            controller.FuelChamber.HasValue
            && controller.EndCap.HasValue
            && controller.PowerBox.HasValue
            && controller.PortEmitter.HasValue
            && controller.ForeEmitter.HasValue
            && controller.StarboardEmitter.HasValue;

        foreach (var part in AllParts(uid, controller))
        {
            if (partQuery.TryGetComponent(part, out var partState))
                partState.Master = uid;
        }

        controller.CurrentlyRescanning = false;

        UpdatePowerDraw(uid, controller);
        UpdateUI(uid, controller);
    }

    private bool ScanPart<T>(EntityUid uid, Vector2i coordinates, Angle? rotation, [NotNullWhen(true)] out EntityUid? part, [NotNullWhen(true)] out T? comp, MapGridComponent? grid = null)
        where T : IComponent
    {
        if (!Resolve(uid, ref grid))
        {
            part = null;
            comp = default;
            return false;
        }

        var compQuery = GetEntityQuery<T>();
        foreach (var entity in grid.GetAnchoredEntities(coordinates))
        {
            if (compQuery.TryGetComponent(entity, out comp)
            && TryComp<ParticleAcceleratorPartComponent>(entity, out var partState) && partState.Master == null
            && (rotation == null || Transform(entity).LocalRotation.EqualsApprox(rotation!.Value.Theta)))
            {
                part = entity;
                return true;
            }
        }

        part = null;
        comp = default;
        return false;
    }

    private void OnComponentShutdown(EntityUid uid, ParticleAcceleratorPartComponent comp, ComponentShutdown args)
    {
        if (EntityManager.EntityExists(comp.Master))
            RescanParts(comp.Master!.Value);
    }

    private void BodyTypeChanged(EntityUid uid, ParticleAcceleratorPartComponent comp, ref PhysicsBodyTypeChangedEvent args)
    {
        if (EntityManager.EntityExists(comp.Master))
            RescanParts(comp.Master!.Value);
    }

    private void OnMoveEvent(EntityUid uid, ParticleAcceleratorPartComponent comp, ref MoveEvent args)
    {
        if (EntityManager.EntityExists(comp.Master))
            RescanParts(comp.Master!.Value);
    }
}
