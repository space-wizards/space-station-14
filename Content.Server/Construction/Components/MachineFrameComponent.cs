using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public class MachineFrameComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public const string PartContainer = "machine_parts";
        public const string BoardContainer = "machine_board";

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

                foreach (var (tagName, info) in TagRequirements)
                {
                    if (_tagProgress[tagName] < info.Amount)
                        return false;
                }

                return true;
            }
        }

        [ViewVariables]
        public bool HasBoard => _boardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        private readonly Dictionary<MachinePart, int> _progress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _materialProgress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _componentProgress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _tagProgress = new();

        [ViewVariables]
        private Dictionary<MachinePart, int> _requirements = new();

        [ViewVariables]
        private Dictionary<string, int> _materialRequirements = new();

        [ViewVariables]
        private Dictionary<string, GenericPartInfo> _componentRequirements = new();

        [ViewVariables]
        private Dictionary<string, GenericPartInfo> _tagRequirements = new();

        [ViewVariables]
        private Container _boardContainer = default!;

        [ViewVariables]
        private Container _partContainer = default!;

        public IReadOnlyDictionary<MachinePart, int> Progress => _progress;

        public IReadOnlyDictionary<string, int> MaterialProgress => _materialProgress;

        public IReadOnlyDictionary<string, int> ComponentProgress => _componentProgress;

        public IReadOnlyDictionary<string, int> TagProgress => _tagProgress;

        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;

        public IReadOnlyDictionary<string, int> MaterialRequirements => _materialRequirements;

        public IReadOnlyDictionary<string, GenericPartInfo> ComponentRequirements => _componentRequirements;

        public IReadOnlyDictionary<string, GenericPartInfo> TagRequirements => _tagRequirements;

        protected override void Initialize()
        {
            base.Initialize();

            _boardContainer = ContainerHelpers.EnsureContainer<Container>(Owner, BoardContainer);
            _partContainer = ContainerHelpers.EnsureContainer<Container>(Owner, PartContainer);
        }

        protected override void Startup()
        {
            base.Startup();

            RegenerateProgress();

            if (_entMan.TryGetComponent<ConstructionComponent?>(Owner, out var construction))
            {
                // Attempt to set pathfinding to the machine node...
                EntitySystem.Get<ConstructionSystem>().SetPathfindingTarget(Owner, "machine", construction);
            }
        }

        private void ResetProgressAndRequirements(MachineBoardComponent machineBoard)
        {
            _requirements = new Dictionary<MachinePart, int>(machineBoard.Requirements);
            _materialRequirements = new Dictionary<string, int>(machineBoard.MaterialIdRequirements);
            _componentRequirements = new Dictionary<string, GenericPartInfo>(machineBoard.ComponentRequirements);
            _tagRequirements = new Dictionary<string, GenericPartInfo>(machineBoard.TagRequirements);

            _progress.Clear();
            _materialProgress.Clear();
            _componentProgress.Clear();
            _tagProgress.Clear();

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

            foreach (var (compName, _) in TagRequirements)
            {
                _tagProgress[compName] = 0;
            }
        }

        public void RegenerateProgress()
        {
            AppearanceComponent? appearance;

            if (!HasBoard)
            {
                if (_entMan.TryGetComponent(Owner, out appearance))
                {
                    appearance.SetData(MachineFrameVisuals.State, 1);
                }

                _requirements.Clear();
                _materialRequirements.Clear();
                _componentRequirements.Clear();
                _tagRequirements.Clear();
                _progress.Clear();
                _materialProgress.Clear();
                _componentProgress.Clear();
                _tagProgress.Clear();

                return;
            }

            var board = _boardContainer.ContainedEntities[0];

            if (!_entMan.TryGetComponent<MachineBoardComponent?>(board, out var machineBoard))
                return;

            if (_entMan.TryGetComponent(Owner, out appearance))
            {
                appearance.SetData(MachineFrameVisuals.State, 2);
            }

            ResetProgressAndRequirements(machineBoard);

            foreach (var part in _partContainer.ContainedEntities)
            {
                if (_entMan.TryGetComponent<MachinePartComponent?>(part, out var machinePart))
                {
                    // Check this is part of the requirements...
                    if (!Requirements.ContainsKey(machinePart.PartType))
                        continue;

                    if (!_progress.ContainsKey(machinePart.PartType))
                        _progress[machinePart.PartType] = 1;
                    else
                        _progress[machinePart.PartType]++;
                }

                if (_entMan.TryGetComponent<StackComponent?>(part, out var stack))
                {
                    var type = stack.StackTypeId;
                    // Check this is part of the requirements...
                    if (!MaterialRequirements.ContainsKey(type))
                        continue;

                    if (!_materialProgress.ContainsKey(type))
                        _materialProgress[type] = 1;
                    else
                        _materialProgress[type]++;
                }

                // I have many regrets.
                foreach (var (compName, _) in ComponentRequirements)
                {
                    var registration = _componentFactory.GetRegistration(compName);

                    if (!_entMan.HasComponent(part, registration.Type))
                        continue;

                    if (!_componentProgress.ContainsKey(compName))
                        _componentProgress[compName] = 1;
                    else
                        _componentProgress[compName]++;
                }

                // I have MANY regrets.
                foreach (var (tagName, _) in TagRequirements)
                {
                    if (!part.HasTag(tagName))
                        continue;

                    if (!_tagProgress.ContainsKey(tagName))
                        _tagProgress[tagName] = 1;
                    else
                        _tagProgress[tagName]++;
                }
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!HasBoard && _entMan.TryGetComponent<MachineBoardComponent?>(eventArgs.Using, out var machineBoard))
            {
                if (eventArgs.Using.TryRemoveFromContainer())
                {
                    // Valid board!
                    _boardContainer.Insert(eventArgs.Using);

                    // Setup requirements and progress...
                    ResetProgressAndRequirements(machineBoard);

                    if (_entMan.TryGetComponent<AppearanceComponent?>(Owner, out var appearance))
                    {
                        appearance.SetData(MachineFrameVisuals.State, 2);
                    }

                    if (_entMan.TryGetComponent(Owner, out ConstructionComponent? construction))
                    {
                        // So prying the components off works correctly.
                        EntitySystem.Get<ConstructionSystem>().ResetEdge(Owner, construction);
                    }

                    return true;
                }
            }
            else if (HasBoard)
            {
                if (_entMan.TryGetComponent<MachinePartComponent?>(eventArgs.Using, out var machinePart))
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

                if (_entMan.TryGetComponent<StackComponent?>(eventArgs.Using, out var stack))
                {
                    var type = stack.StackTypeId;
                    if (!MaterialRequirements.ContainsKey(type))
                        return false;

                    if (_materialProgress[type] == MaterialRequirements[type])
                        return false;

                    var needed = MaterialRequirements[type] - _materialProgress[type];
                    var count = stack.Count;

                    if (count < needed)
                    {
                        if(!_partContainer.Insert(stack.Owner))
                            return false;

                        _materialProgress[type] += count;
                        return true;
                    }

                    var splitStack = EntitySystem.Get<StackSystem>().Split(eventArgs.Using, needed, _entMan.GetComponent<TransformComponent>(Owner).Coordinates, stack);

                    if (splitStack == null)
                        return false;

                    if(!_partContainer.Insert(splitStack.Value))
                        return false;

                    _materialProgress[type] += needed;
                    return true;
                }

                foreach (var (compName, info) in ComponentRequirements)
                {
                    if (_componentProgress[compName] >= info.Amount)
                        continue;

                    var registration = _componentFactory.GetRegistration(compName);

                    if (!_entMan.HasComponent(eventArgs.Using, registration.Type))
                        continue;

                    if (!eventArgs.Using.TryRemoveFromContainer() || !_partContainer.Insert(eventArgs.Using)) continue;
                    _componentProgress[compName]++;
                    return true;
                }

                foreach (var (tagName, info) in TagRequirements)
                {
                    if (_tagProgress[tagName] >= info.Amount)
                        continue;

                    if (!eventArgs.Using.HasTag(tagName))
                        continue;

                    if (!eventArgs.Using.TryRemoveFromContainer() || !_partContainer.Insert(eventArgs.Using)) continue;
                    _tagProgress[tagName]++;
                    return true;
                }
            }

            return false;
        }
    }

    [DataDefinition]
    public class MachineDeconstructedEvent : EntityEventArgs
    {
    }
}
