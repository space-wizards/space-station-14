using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Server.Construction;

public sealed class MachineFrameSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MachineFrameComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MachineFrameComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MachineFrameComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MachineFrameComponent, ExaminedEvent>(OnMachineFrameExamined);
    }

    private void OnInit(EntityUid uid, MachineFrameComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);
    }

    private void OnStartup(EntityUid uid, MachineFrameComponent component, ComponentStartup args)
    {
        RegenerateProgress(component);

        if (TryComp<ConstructionComponent>(uid, out var construction))
        {
            // Attempt to set pathfinding to the machine node...
            _construction.SetPathfindingTarget(uid, "machine", construction);
        }
    }

    private void OnInteractUsing(EntityUid uid, MachineFrameComponent component, InteractUsingEvent args)
    {
        if (!component.HasBoard && TryComp<MachineBoardComponent?>(args.Used, out var machineBoard))
        {
            if (_container.TryRemoveFromContainer(args.Used))
            {
                // Valid board!
                component.BoardContainer.Insert(args.Used);

                // Setup requirements and progress...
                ResetProgressAndRequirements(component, machineBoard);

                if (TryComp(uid, out ConstructionComponent? construction))
                {
                    // So prying the components off works correctly.
                    _construction.ResetEdge(uid, construction);
                }
            }
        }
        else if (component.HasBoard)
        {
            if (TryComp<MachinePartComponent>(args.Used, out var machinePart))
            {
                if (!component.Requirements.ContainsKey(machinePart.PartType))
                    return;

                if (component.Progress[machinePart.PartType] != component.Requirements[machinePart.PartType]
                    && _container.TryRemoveFromContainer(args.Used) && component.PartContainer.Insert(args.Used))
                {
                    component.Progress[machinePart.PartType]++;
                    args.Handled = true;
                    return;
                }
            }

            if (TryComp<StackComponent?>(args.Used, out var stack))
            {
                var type = stack.StackTypeId;
                if (type == null)
                    return;
                if (!component.MaterialRequirements.ContainsKey(type))
                    return;

                if (component.MaterialProgress[type] == component.MaterialRequirements[type])
                    return;

                var needed = component.MaterialRequirements[type] - component.MaterialProgress[type];
                var count = stack.Count;

                if (count < needed)
                {
                    if (!component.PartContainer.Insert(stack.Owner))
                        return;

                    component.MaterialProgress[type] += count;
                    args.Handled = true;
                    return;
                }

                var splitStack = _stack.Split(args.Used, needed,
                    Comp<TransformComponent>(uid).Coordinates, stack);

                if (splitStack == null)
                    return;

                if (!component.PartContainer.Insert(splitStack.Value))
                    return;

                component.MaterialProgress[type] += needed;
                args.Handled = true;
                return;
            }

            foreach (var (compName, info) in component.ComponentRequirements)
            {
                if (component.ComponentProgress[compName] >= info.Amount)
                    continue;

                var registration = _factory.GetRegistration(compName);

                if (!HasComp(args.Used, registration.Type))
                    continue;

                if (!_container.TryRemoveFromContainer(args.Used) || !component.PartContainer.Insert(args.Used))
                    continue;
                component.ComponentProgress[compName]++;
                args.Handled = true;
                return;
            }

            foreach (var (tagName, info) in component.TagRequirements)
            {
                if (component.TagProgress[tagName] >= info.Amount)
                    continue;

                if (!_tag.HasTag(args.Used, tagName))
                    continue;

                if (!_container.TryRemoveFromContainer(args.Used) || !component.PartContainer.Insert(args.Used))
                    continue;
                component.TagProgress[tagName]++;
                args.Handled = true;
                return;
            }
        }
    }

    public bool IsComplete(MachineFrameComponent component)
    {
        if (!component.HasBoard)
            return false;

        foreach (var (part, amount) in component.Requirements)
        {
            if (component.Progress[part] < amount)
                return false;
        }

        foreach (var (type, amount) in component.MaterialRequirements)
        {
            if (component.MaterialProgress[type] < amount)
                return false;
        }

        foreach (var (compName, info) in component.ComponentRequirements)
        {
            if (component.ComponentProgress[compName] < info.Amount)
                return false;
        }

        foreach (var (tagName, info) in component.TagRequirements)
        {
            if (component.TagProgress[tagName] < info.Amount)
                return false;
        }

        return true;
    }

    public void ResetProgressAndRequirements(MachineFrameComponent component, MachineBoardComponent machineBoard)
    {
        component.Requirements = new Dictionary<string, int>(machineBoard.Requirements);
        component.MaterialRequirements = new Dictionary<string, int>(machineBoard.MaterialIdRequirements);
        component.ComponentRequirements = new Dictionary<string, GenericPartInfo>(machineBoard.ComponentRequirements);
        component.TagRequirements = new Dictionary<string, GenericPartInfo>(machineBoard.TagRequirements);

        component.Progress.Clear();
        component.MaterialProgress.Clear();
        component.ComponentProgress.Clear();
        component.TagProgress.Clear();

        foreach (var (machinePart, _) in component.Requirements)
        {
            component.Progress[machinePart] = 0;
        }

        foreach (var (stackType, _) in component.MaterialRequirements)
        {
            component.MaterialProgress[stackType] = 0;
        }

        foreach (var (compName, _) in component.ComponentRequirements)
        {
            component.ComponentProgress[compName] = 0;
        }

        foreach (var (compName, _) in component.TagRequirements)
        {
            component.TagProgress[compName] = 0;
        }
    }

    public void RegenerateProgress(MachineFrameComponent component)
    {
        if (!component.HasBoard)
        {
            component.TagRequirements.Clear();
            component.MaterialRequirements.Clear();
            component.ComponentRequirements.Clear();
            component.TagRequirements.Clear();
            component.Progress.Clear();
            component.MaterialProgress.Clear();
            component.ComponentProgress.Clear();
            component.TagProgress.Clear();

            return;
        }

        var board = component.BoardContainer.ContainedEntities[0];

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
            return;

        ResetProgressAndRequirements(component, machineBoard);

        foreach (var part in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent>(part, out var machinePart))
            {
                // Check this is part of the requirements...
                if (!component.Requirements.ContainsKey(machinePart.PartType))
                    continue;

                if (!component.Progress.ContainsKey(machinePart.PartType))
                    component.Progress[machinePart.PartType] = 1;
                else
                    component.Progress[machinePart.PartType]++;
            }

            if (TryComp<StackComponent>(part, out var stack))
            {
                var type = stack.StackTypeId;
                // Check this is part of the requirements...
                if (type == null)
                    continue;
                if (!component.MaterialRequirements.ContainsKey(type))
                    continue;

                if (!component.MaterialProgress.ContainsKey(type))
                    component.MaterialProgress[type] = 1;
                else
                    component.MaterialProgress[type]++;
            }

            // I have many regrets.
            foreach (var (compName, _) in component.ComponentRequirements)
            {
                var registration = _factory.GetRegistration(compName);

                if (!HasComp(part, registration.Type))
                    continue;

                if (!component.ComponentProgress.ContainsKey(compName))
                    component.ComponentProgress[compName] = 1;
                else
                    component.ComponentProgress[compName]++;
            }

            // I have MANY regrets.
            foreach (var (tagName, _) in component.TagRequirements)
            {
                if (!_tag.HasTag(part, tagName))
                    continue;

                if (!component.TagProgress.ContainsKey(tagName))
                    component.TagProgress[tagName] = 1;
                else
                    component.TagProgress[tagName]++;
            }
        }
    }
    private void OnMachineFrameExamined(EntityUid uid, MachineFrameComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        if (component.HasBoard)
            args.PushMarkup(Loc.GetString("machine-frame-component-on-examine-label", ("board", EntityManager.GetComponent<MetaDataComponent>(component.BoardContainer.ContainedEntities[0]).EntityName)));
    }
}
