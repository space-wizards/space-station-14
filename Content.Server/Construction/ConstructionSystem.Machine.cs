using System;
using System.Collections.Generic;
using Content.Server.Construction.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    private void InitializeMachines()
    {
        SubscribeLocalEvent<MachineComponent, ComponentInit>(OnMachineInit);
        SubscribeLocalEvent<MachineComponent, MapInitEvent>(OnMachineMapInit);
    }

    private void OnMachineInit(EntityUid uid, MachineComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainer);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainer);
    }

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(component);
        RefreshParts(component);
    }

    public List<MachinePartComponent> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartComponent>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent?>(entity, out var machinePart))
                parts.Add(machinePart);
        }

        return parts;
    }

    public void RefreshParts(MachineComponent component)
    {
        EntityManager.EventBus.RaiseLocalEvent(component.Owner, new RefreshPartsEvent()
        {
            Parts = GetAllParts(component),
        });
    }

    public void CreateBoardAndStockParts(MachineComponent component)
    {
        // Entity might not be initialized yet.
        var boardContainer = _container.EnsureContainer<Container>(component.Owner, MachineFrameComponent.BoardContainer);
        var partContainer = _container.EnsureContainer<Container>(component.Owner, MachineFrameComponent.PartContainer);

        if (string.IsNullOrEmpty(component.BoardPrototype))
            return;

        // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
        if (boardContainer.ContainedEntities.Count > 0)
            return;

        var board = EntityManager.SpawnEntity(component.BoardPrototype, Transform(component.Owner).Coordinates);

        if (!component.BoardContainer.Insert(board))
        {
            throw new Exception($"Couldn't insert board with prototype {component.BoardPrototype} to machine with prototype {MetaData(component.Owner).EntityPrototype?.ID ?? "N/A"}!");
        }

        if (!TryComp<MachineBoardComponent?>(board, out var machineBoard))
        {
            throw new Exception($"Entity with prototype {component.BoardPrototype} doesn't have a {nameof(MachineBoardComponent)}!");
        }

        foreach (var (part, amount) in machineBoard.Requirements)
        {
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(MachinePartComponent.Prototypes[part], Transform(component.Owner).Coordinates);

                if (!partContainer.Insert(p))
                    throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {MetaData(component.Owner).EntityPrototype?.ID ?? "N/A"}!");
            }
        }

        foreach (var (stackType, amount) in machineBoard.MaterialRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, Transform(component.Owner).Coordinates);

            if (!partContainer.Insert(stack))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {MetaData(component.Owner).EntityPrototype?.ID ?? "N/A"}");
        }

        foreach (var (compName, info) in machineBoard.ComponentRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                var c = EntityManager.SpawnEntity(info.DefaultPrototype, Transform(component.Owner).Coordinates);

                if(!partContainer.Insert(c))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {MetaData(component.Owner).EntityPrototype?.ID ?? "N/A"}");
            }
        }

        foreach (var (tagName, info) in machineBoard.TagRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                var c = EntityManager.SpawnEntity(info.DefaultPrototype, Transform(component.Owner).Coordinates);

                if(!partContainer.Insert(c))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {MetaData(component.Owner).EntityPrototype?.ID ?? "N/A"}");
            }
        }
    }
}

public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartComponent> Parts = new List<MachinePartComponent>();
}
