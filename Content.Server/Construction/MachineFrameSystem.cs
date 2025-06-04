using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction;

public sealed class MachineFrameSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

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

        if (TryComp<ConstructionComponent>(uid, out var construction) && construction.TargetNode == null)
        {
            // Attempt to set pathfinding to the machine node...
            _construction.SetPathfindingTarget(uid, "machine", construction);
        }
    }

    private void OnInteractUsing(EntityUid uid, MachineFrameComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasBoard)
        {
            if (TryInsertBoard(uid, args.Used, component))
                args.Handled = true;
            return;
        }

        // If this changes in the future, then RegenerateProgress() also needs to be updated.
        // Note that one entity is ALLOWED to satisfy more than one kind of component or tag requirements. This is
        // necessary in order to avoid weird entity-ordering shenanigans in RegenerateProgress().
        if (TryComp<StackComponent>(args.Used, out var stack))
        {
            if (TryInsertStack(uid, args.Used, component, stack))
                args.Handled = true;
            return;
        }

        // Handle component requirements
        foreach (var (compName, info) in component.ComponentRequirements)
        {
            if (component.ComponentProgress[compName] >= info.Amount)
                continue;

            var registration = Factory.GetRegistration(compName);

            if (!HasComp(args.Used, registration.Type))
                continue;

            // Insert the entity, if it hasn't already been inserted
            if (!args.Handled)
            {
                if (!_container.TryRemoveFromContainer(args.Used))
                    return;

                args.Handled = true;
                if (!_container.Insert(args.Used, component.PartContainer))
                    return;
            }

            component.ComponentProgress[compName]++;

            if (IsComplete(component))
            {
                _popupSystem.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), uid);
                return;
            }
        }

        // Handle tag requirements
        if (!TryComp<TagComponent>(args.Used, out var tagComp))
            return;

        foreach (var (tagName, info) in component.TagRequirements)
        {
            if (component.TagProgress[tagName] >= info.Amount)
                continue;

            if (!_tag.HasTag(tagComp, tagName))
                continue;

            // Insert the entity, if it hasn't already been inserted
            if (!args.Handled)
            {
                if (!_container.TryRemoveFromContainer(args.Used))
                    return;

                args.Handled = true;
                if (!_container.Insert(args.Used, component.PartContainer))
                    return;
            }

            component.TagProgress[tagName]++;
            args.Handled = true;

            if (IsComplete(component))
            {
                _popupSystem.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), uid);
                return;
            }
        }
    }

    /// <returns>Whether or not the function had any effect. Does not indicate success.</returns>
    private bool TryInsertBoard(EntityUid uid, EntityUid used, MachineFrameComponent component)
    {
        if (!TryComp<MachineBoardComponent>(used, out var machineBoard))
            return false;

        if (!_container.TryRemoveFromContainer(used))
            return false;

        if (!_container.Insert(used, component.BoardContainer))
            return true;

        ResetProgressAndRequirements(component, machineBoard);

        // Reset edge so that prying the components off works correctly.
        if (TryComp(uid, out ConstructionComponent? construction))
            _construction.ResetEdge(uid, construction);

        return true;
    }

    /// <returns>Whether or not the function had any effect. Does not indicate success.</returns>
    private bool TryInsertStack(EntityUid uid, EntityUid used, MachineFrameComponent component, StackComponent stack)
    {
        var type = stack.StackTypeId;

        if (!component.MaterialRequirements.ContainsKey(type))
            return false;

        var progress = component.MaterialProgress[type];
        var requirement = component.MaterialRequirements[type];
        var needed = requirement - progress;

        if (needed <= 0)
            return false;

        var count = stack.Count;
        if (count < needed)
        {
            if (!_container.TryRemoveFromContainer(used))
                return false;

            if (!_container.Insert(used, component.PartContainer))
                return true;

            component.MaterialProgress[type] += count;
            return true;
        }

        var splitStack = _stack.Split(used, needed, Transform(uid).Coordinates, stack);

        if (splitStack == null)
            return false;

        if (!_container.Insert(splitStack.Value, component.PartContainer))
            return true;

        component.MaterialProgress[type] += needed;
        if (IsComplete(component))
            _popupSystem.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), uid);

        return true;
    }

    public bool IsComplete(MachineFrameComponent component)
    {
        if (!component.HasBoard)
            return false;

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
        component.MaterialRequirements = new Dictionary<ProtoId<StackPrototype>, int>(machineBoard.StackRequirements);
        component.ComponentRequirements = new Dictionary<string, GenericPartInfo>(machineBoard.ComponentRequirements);
        component.TagRequirements = new Dictionary<ProtoId<TagPrototype>, GenericPartInfo>(machineBoard.TagRequirements);

        component.MaterialProgress.Clear();
        component.ComponentProgress.Clear();
        component.TagProgress.Clear();

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
            component.MaterialProgress.Clear();
            component.ComponentProgress.Clear();
            component.TagProgress.Clear();

            return;
        }

        var board = component.BoardContainer.ContainedEntities[0];

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
            return;

        ResetProgressAndRequirements(component, machineBoard);

        // If the following code is updated, you need to make sure that it matches the logic in OnInteractUsing()

        foreach (var part in component.PartContainer.ContainedEntities)
        {
            if (TryComp<StackComponent>(part, out var stack))
            {
                var type = stack.StackTypeId;

                if (!component.MaterialRequirements.ContainsKey(type))
                    continue;

                if (!component.MaterialProgress.ContainsKey(type))
                    component.MaterialProgress[type] = stack.Count;
                else
                    component.MaterialProgress[type] += stack.Count;

                continue;
            }

            // I have many regrets.
            foreach (var (compName, _) in component.ComponentRequirements)
            {
                var registration = Factory.GetRegistration(compName);

                if (!HasComp(part, registration.Type))
                    continue;

                if (!component.ComponentProgress.TryAdd(compName, 1))
                    component.ComponentProgress[compName]++;
            }

            if (!TryComp<TagComponent>(part, out var tagComp))
                continue;

            // I have MANY regrets.
            foreach (var tagName in component.TagRequirements.Keys)
            {
                if (!_tag.HasTag(tagComp, tagName))
                    continue;

                if (!component.TagProgress.TryAdd(tagName, 1))
                    component.TagProgress[tagName]++;
            }
        }
    }
    private void OnMachineFrameExamined(EntityUid uid, MachineFrameComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !component.HasBoard)
            return;

        var board = component.BoardContainer.ContainedEntities[0];
        args.PushMarkup(Loc.GetString("machine-frame-component-on-examine-label", ("board", Name(board))));
    }
}
