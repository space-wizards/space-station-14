using Content.Shared.Machines.Components;
using Content.Server.ParticleAccelerator.Components;
using JetBrains.Annotations;
using Content.Shared.ParticleAccelerator;

namespace Content.Server.ParticleAccelerator.EntitySystems;

[UsedImplicitly]
public sealed partial class ParticleAcceleratorSystem
{
    public void ValidateEmitter(Enum part,
        ParticleAcceleratorEmitterType type,
        Entity<MultipartMachineComponent> machine)
    {
        var emitterEnt = _multipartMachine.GetPartEntity(machine.AsNullable(), part);
        if (!TryComp<ParticleAcceleratorEmitterComponent>(emitterEnt, out var partState))
        {
            return;
        }

        if (partState.Type != type)
        {
            _multipartMachine.ClearPartEntity(machine.AsNullable(), part);
            return;
        }

        return;
    }

    public void RescanParts(EntityUid uid, EntityUid? user = null, ParticleAcceleratorControlBoxComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        if (controller.CurrentlyRescanning)
            return;

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        var machine = new Entity<MultipartMachineComponent>(uid, machineComp);
        _multipartMachine.Rescan(machine, user); // Raises an event if state has changed
    }

    private void ValidateMachine(Entity<ParticleAcceleratorControlBoxComponent> ent, EntityUid? user = null)
    {
        if (!TryComp<MultipartMachineComponent>(ent, out var machineComp))
            return;

        var machine = new Entity<MultipartMachineComponent>(ent, machineComp);

        // Determine if the proper emitters are in the proper spots
        ValidateEmitter(AcceleratorParts.PortEmitter, ParticleAcceleratorEmitterType.Port, machine);
        ValidateEmitter(AcceleratorParts.ForeEmitter, ParticleAcceleratorEmitterType.Fore, machine);
        ValidateEmitter(AcceleratorParts.StarboardEmitter, ParticleAcceleratorEmitterType.Starboard, machine);

        if (!_multipartMachine.Assembled(machine.AsNullable()))
        {
            // One or more of the emitters are in the incorrect places
            SwitchOff(ent, user, ent.Comp);
            return;
        }

        ent.Comp.CurrentlyRescanning = false;

        var partQuery = GetEntityQuery<ParticleAcceleratorPartComponent>();
        foreach (var part in machine.Comp.Parts.Values)
        {
            if (partQuery.TryGetComponent(GetEntity(part.Entity), out var partData))
                partData.Master = ent;
        }

        UpdatePowerDraw(ent, ent.Comp);
        UpdateUI(ent, ent.Comp);
    }
}
