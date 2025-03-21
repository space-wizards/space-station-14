using Content.Shared.Machines.Components;
using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;
using Robust.Shared.Physics.Events;
using Content.Server.Construction.Components;
using System.Reflection.PortableExecutable;

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

    public bool ValidateEmitter(string name,
        ParticleAcceleratorEmitterType type,
        Entity<MultipartMachineComponent> machine)
    {
        var emitterEnt = _multipartMachine.GetPartEntity(machine.AsNullable(), name);
        if (!TryComp<ParticleAcceleratorEmitterComponent>(emitterEnt, out var partState))
        {
            return false;
        }

        return partState.Type == type;
    }

    public void RescanParts(EntityUid uid, EntityUid? user = null, ParticleAcceleratorControlBoxComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        if (controller.CurrentlyRescanning)
            return;

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        controller.Assembled = false;

        var machine = new Entity<MultipartMachineComponent>(uid, machineComp);
        if (!_multipartMachine.Rescan(machine))
        {
            // All entities are not in the right place
            SwitchOff(uid, user, controller);
            return;
        }

        // Determine if the proper emitters are in the proper spots
        if (!ValidateEmitter("PortEmitter", ParticleAcceleratorEmitterType.Port, machine) ||
            !ValidateEmitter("ForeEmitter", ParticleAcceleratorEmitterType.Fore, machine) ||
            !ValidateEmitter("StarboardEmitter", ParticleAcceleratorEmitterType.Starboard, machine))
        {
            // One or more of the emitters are in the incorrect places
            SwitchOff(uid, user, controller);
            return;
        }

        controller.Assembled = true;
        controller.CurrentlyRescanning = false;

        var partQuery = GetEntityQuery<ParticleAcceleratorPartComponent>();
        foreach (var part in machine.Comp.Parts.Values)
        {
            if (partQuery.TryGetComponent(GetEntity(part.Entity), out var partData))
                partData.Master = uid;
        }

        UpdatePowerDraw(uid, controller);
        UpdateUI(uid, controller);
    }

    private void OnComponentShutdown(EntityUid uid, ParticleAcceleratorPartComponent comp, ComponentShutdown args)
    {
        if (Exists(comp.Master))
            RescanParts(comp.Master!.Value);
    }

    private void BodyTypeChanged(EntityUid uid, ParticleAcceleratorPartComponent comp, ref PhysicsBodyTypeChangedEvent args)
    {
        if (Exists(comp.Master))
            RescanParts(comp.Master!.Value);
    }

    private void OnMoveEvent(EntityUid uid, ParticleAcceleratorPartComponent comp, ref MoveEvent args)
    {
        if (Exists(comp.Master))
            RescanParts(comp.Master!.Value);
    }
}
