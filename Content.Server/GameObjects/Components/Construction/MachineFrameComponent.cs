using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineFrameComponent : Component, IInteractUsing
    {
        public const string PartContainer = "machine_parts";
        public const string BoardContainer = "machine_board";

        public override string Name => "MachineFrame";

        [ViewVariables]
        public bool IsComplete
        {
            get
            {
                if (!HasBoard || _requirements == null || _materialRequirements == null)
                    return false;

                foreach (var (part, amount) in _requirements)
                {
                    if (_progress[part] < amount)
                        return false;
                }

                foreach (var (type, amount) in _materialRequirements)
                {
                    if (_materialProgress[type] < amount)
                        return false;
                }

                return true;
            }
        }

        [ViewVariables]
        public bool HasBoard => _boardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        private IReadOnlyDictionary<MachinePart, int> _requirements;

        [ViewVariables]
        private IReadOnlyDictionary<StackType, int> _materialRequirements;

        [ViewVariables]
        private Dictionary<MachinePart, int> _progress;

        [ViewVariables]
        private Dictionary<StackType, int> _materialProgress;

        [ViewVariables]
        private Container _boardContainer;

        [ViewVariables]
        private Container _partContainer;

        public override void Initialize()
        {
            base.Initialize();

            _boardContainer = ContainerManagerComponent.Ensure<Container>(BoardContainer, Owner);
            _partContainer = ContainerManagerComponent.Ensure<Container>(PartContainer, Owner);

            RegenerateProgress();
        }

        public void RegenerateProgress()
        {
            if (!HasBoard)
            {
                if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
                {
                    sprite.LayerSetState(0, "box_1");
                }

                _requirements = null;
                _materialRequirements = null;
                _progress = null;
                _materialProgress = null;

                return;
            }

            _progress = new Dictionary<MachinePart, int>();
            _materialProgress = new Dictionary<StackType, int>();
            foreach (var part in _partContainer.ContainedEntities)
            {
                if (part.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    // Check this is part of the requirements...
                    if (!_requirements.ContainsKey(machinePart.PartType))
                        continue;

                    if (!_progress.ContainsKey(machinePart.PartType))
                        _progress[machinePart.PartType] = 1;
                    else
                        _progress[machinePart.PartType]++;
                }

                if (!part.TryGetComponent<StackComponent>(out var stack))
                    continue;

                var type = (StackType)stack.StackType;
                // Check this is part of the requirements...
                if (!_materialRequirements.ContainsKey(type))
                    continue;

                if (!_materialProgress.ContainsKey(type))
                    _materialProgress[type] = 1;
                else
                    _materialProgress[type]++;
            }
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!HasBoard && eventArgs.Using.TryGetComponent<MachineBoardComponent>(out var machineBoard))
            {
                if (eventArgs.Using.TryRemoveFromContainer())
                {
                    // Valid board!
                    _boardContainer.Insert(eventArgs.Using);

                    // Setup requirements and progress...
                    _requirements = machineBoard.Requirements;
                    _materialRequirements = machineBoard.MaterialRequirements;
                    _progress = new Dictionary<MachinePart, int>();
                    _materialProgress = new Dictionary<StackType, int>();

                    foreach (var (machinePart, _) in _requirements)
                    {
                        _progress[machinePart] = 0;
                    }

                    foreach (var (stackType, _) in _materialRequirements)
                    {
                        _materialProgress[stackType] = 0;
                    }

                    if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
                    {
                        sprite.LayerSetState(0, "box_2");
                    }

                    return true;
                }
            }
            else if (HasBoard)
            {
                if (eventArgs.Using.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    if (!_requirements.ContainsKey(machinePart.PartType))
                        return false;

                    if (_progress[machinePart.PartType] != _requirements[machinePart.PartType]
                    && eventArgs.Using.TryRemoveFromContainer() && _partContainer.Insert(eventArgs.Using))
                    {
                        _progress[machinePart.PartType]++;
                        return true;
                    }
                }

                if (eventArgs.Using.TryGetComponent<StackComponent>(out var stack))
                {
                    var type = (StackType) stack.StackType;
                    if (!_materialRequirements.ContainsKey(type))
                        return false;

                    if (_materialProgress[type] == _materialRequirements[type])
                        return false;

                    var needed = _materialRequirements[type] - _materialProgress[type];
                    var count = stack.Count;

                    if (count < needed && stack.Use(count))
                    {
                        _materialProgress[type] += count;
                        return true;
                    }

                    if (!stack.Use(needed))
                        return false;

                    _materialProgress[type] += needed;
                    return true;
                }
            }

            return false;
        }
    }
}
