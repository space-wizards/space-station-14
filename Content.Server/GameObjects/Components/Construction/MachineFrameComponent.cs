using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Construction;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineFrameComponent : Component, IInteractUsing
    {
        [Dependency] private IComponentFactory _componentFactory = default!;

        public const string PartContainer = "machine_parts";
        public const string BoardContainer = "machine_board";

        public override string Name => "MachineFrame";

        [ViewVariables]
        public bool IsComplete
        {
            get
            {
                if (!HasBoard || Requirements == null || MaterialRequirements == null)
                    return false;

                foreach (var (part, amount) in Requirements)
                {
                    if (_progress[part] < amount)
                        return false;
                }

                foreach (var (type, amount) in MaterialRequirements)
                {
                    if (_materialProgress[type] < amount)
                        return false;
                }

                foreach (var (compName, info) in ComponentRequirements)
                {
                    if (_componentProgress[compName] < info.Amount)
                        return false;
                }

                return true;
            }
        }

        [ViewVariables]
        public bool HasBoard => _boardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        private Dictionary<MachinePart, int> _progress;

        [ViewVariables]
        private Dictionary<StackType, int> _materialProgress;

        [ViewVariables]
        private Dictionary<string, int> _componentProgress;

        [ViewVariables]
        private Container _boardContainer;

        [ViewVariables]
        private Container _partContainer;

        [ViewVariables]
        public IReadOnlyDictionary<MachinePart, int> Requirements { get; private set; }

        [ViewVariables]
        public IReadOnlyDictionary<StackType, int> MaterialRequirements { get; private set; }

        [ViewVariables]
        public IReadOnlyDictionary<string, ComponentPartInfo> ComponentRequirements { get; private set; }

        public IReadOnlyDictionary<MachinePart, int> Progress => _progress;
        public IReadOnlyDictionary<StackType, int> MaterialProgress => _materialProgress;
        public IReadOnlyDictionary<string, int> ComponentProgress => _componentProgress;

        public override void Initialize()
        {
            base.Initialize();

            _boardContainer = ContainerManagerComponent.Ensure<Container>(BoardContainer, Owner);
            _partContainer = ContainerManagerComponent.Ensure<Container>(PartContainer, Owner);
        }

        protected override void Startup()
        {
            base.Startup();

            RegenerateProgress();

            if (Owner.TryGetComponent<ConstructionComponent>(out var construction))
            {
                // Attempt to set pathfinding to the machine node...
                construction.SetNewTarget("machine");
            }
        }

        private void ResetProgressAndRequirements(MachineBoardComponent machineBoard)
        {
            Requirements = machineBoard.Requirements;
            MaterialRequirements = machineBoard.MaterialRequirements;
            ComponentRequirements = machineBoard.ComponentRequirements;
            _progress = new Dictionary<MachinePart, int>();
            _materialProgress = new Dictionary<StackType, int>();
            _componentProgress = new Dictionary<string, int>();

            foreach (var (machinePart, _) in Requirements)
            {
                _progress[machinePart] = 0;
            }

            foreach (var (stackType, _) in MaterialRequirements)
            {
                _materialProgress[stackType] = 0;
            }

            foreach (var (compName, _) in ComponentRequirements)
            {
                _componentProgress[compName] = 0;
            }
        }

        public void RegenerateProgress()
        {
            AppearanceComponent appearance;

            if (!HasBoard)
            {
                if (Owner.TryGetComponent(out appearance))
                {
                    appearance.SetData(MachineFrameVisuals.State, 1);
                }

                Requirements = null;
                MaterialRequirements = null;
                ComponentRequirements = null;
                _progress = null;
                _materialProgress = null;
                _componentProgress = null;

                return;
            }

            var board = _boardContainer.ContainedEntities[0];

            if (!board.TryGetComponent<MachineBoardComponent>(out var machineBoard))
                return;

            if (Owner.TryGetComponent(out appearance))
            {
                appearance.SetData(MachineFrameVisuals.State, 2);
            }

            ResetProgressAndRequirements(machineBoard);

            foreach (var part in _partContainer.ContainedEntities)
            {
                if (part.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    // Check this is part of the requirements...
                    if (!Requirements.ContainsKey(machinePart.PartType))
                        continue;

                    if (!_progress.ContainsKey(machinePart.PartType))
                        _progress[machinePart.PartType] = 1;
                    else
                        _progress[machinePart.PartType]++;
                }

                if (part.TryGetComponent<StackComponent>(out var stack))
                {
                    var type = (StackType) stack.StackType;
                    // Check this is part of the requirements...
                    if (!MaterialRequirements.ContainsKey(type))
                        continue;

                    if (!_materialProgress.ContainsKey(type))
                        _materialProgress[type] = 1;
                    else
                        _materialProgress[type]++;
                }

                // I have many regrets.
                foreach (var (compName, amount) in ComponentRequirements)
                {
                    var registration = _componentFactory.GetRegistration(compName);

                    if (!part.HasComponent(registration.Type))
                        continue;

                    if (!_componentProgress.ContainsKey(compName))
                        _componentProgress[compName] = 1;
                    else
                        _componentProgress[compName]++;
                }
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!HasBoard && eventArgs.Using.TryGetComponent<MachineBoardComponent>(out var machineBoard))
            {
                if (eventArgs.Using.TryRemoveFromContainer())
                {
                    // Valid board!
                    _boardContainer.Insert(eventArgs.Using);

                    // Setup requirements and progress...
                    ResetProgressAndRequirements(machineBoard);

                    if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
                    {
                        appearance.SetData(MachineFrameVisuals.State, 2);
                    }

                    if (Owner.TryGetComponent(out ConstructionComponent construction))
                    {
                        // So prying the components off works correctly.
                        construction.ResetEdge();
                    }

                    return true;
                }
            }
            else if (HasBoard)
            {
                if (eventArgs.Using.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    if (!Requirements.ContainsKey(machinePart.PartType))
                        return false;

                    if (_progress[machinePart.PartType] != Requirements[machinePart.PartType]
                    && eventArgs.Using.TryRemoveFromContainer() && _partContainer.Insert(eventArgs.Using))
                    {
                        _progress[machinePart.PartType]++;
                        return true;
                    }
                }

                if (eventArgs.Using.TryGetComponent<StackComponent>(out var stack))
                {
                    var type = (StackType) stack.StackType;
                    if (!MaterialRequirements.ContainsKey(type))
                        return false;

                    if (_materialProgress[type] == MaterialRequirements[type])
                        return false;

                    var needed = MaterialRequirements[type] - _materialProgress[type];
                    var count = stack.Count;

                    if (count < needed && stack.Split(count, Owner.Transform.Coordinates, out var newStack))
                    {
                        _materialProgress[type] += count;
                        return true;
                    }

                    if (!stack.Split(needed, Owner.Transform.Coordinates, out newStack))
                        return false;

                    if(!_partContainer.Insert(newStack))
                        return false;

                    _materialProgress[type] += needed;
                    return true;
                }

                foreach (var (compName, info) in ComponentRequirements)
                {
                    if (_componentProgress[compName] >= info.Amount)
                        continue;

                    var registration = _componentFactory.GetRegistration(compName);

                    if (!eventArgs.Using.HasComponent(registration.Type))
                        continue;

                    if (!eventArgs.Using.TryRemoveFromContainer() || !_partContainer.Insert(eventArgs.Using)) continue;
                    _componentProgress[compName]++;
                    return true;
                }
            }

            return false;
        }
    }
}
