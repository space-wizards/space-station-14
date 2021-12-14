using System;
using System.Collections.Generic;
using Content.Server.Stack;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public class MachineComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Machine";

        [DataField("board")]
        public string? BoardPrototype { get; private set; }

        private Container _boardContainer = default!;
        private Container _partContainer = default!;

        protected override void Initialize()
        {
            base.Initialize();

            _boardContainer = Owner.EnsureContainer<Container>(MachineFrameComponent.BoardContainer);
            _partContainer = Owner.EnsureContainer<Container>(MachineFrameComponent.PartContainer);
        }

        public IEnumerable<MachinePartComponent> GetAllParts()
        {
            foreach (var entity in _partContainer.ContainedEntities)
            {
                if (_entMan.TryGetComponent<MachinePartComponent?>(entity, out var machinePart))
                    yield return machinePart;
            }
        }

        public void RefreshParts()
        {
            foreach (var refreshable in _entMan.GetComponents<IRefreshParts>(Owner))
            {
                refreshable.RefreshParts(GetAllParts());
            }
        }

        public void CreateBoardAndStockParts()
        {
            // Entity might not be initialized yet.
            var boardContainer = Owner.EnsureContainer<Container>(MachineFrameComponent.BoardContainer, out var existedBoard);
            var partContainer = Owner.EnsureContainer<Container>(MachineFrameComponent.PartContainer, out var existedParts);

            if (string.IsNullOrEmpty(BoardPrototype))
                return;

            var entityManager = _entMan;

            if (existedBoard || existedParts)
            {
                // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
                if (boardContainer.ContainedEntities.Count > 0)
                    return;
            }

            var board = entityManager.SpawnEntity(BoardPrototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

            if (!_boardContainer.Insert(board))
            {
                throw new Exception($"Couldn't insert board with prototype {BoardPrototype} to machine with prototype {_entMan.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID ?? "N/A"}!");
            }

            if (!_entMan.TryGetComponent<MachineBoardComponent?>(board, out var machineBoard))
            {
                throw new Exception($"Entity with prototype {BoardPrototype} doesn't have a {nameof(MachineBoardComponent)}!");
            }

            foreach (var (part, amount) in machineBoard.Requirements)
            {
                for (var i = 0; i < amount; i++)
                {
                    var p = entityManager.SpawnEntity(MachinePartComponent.Prototypes[part], _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

                    if (!partContainer.Insert(p))
                        throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {_entMan.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID ?? "N/A"}!");
                }
            }

            foreach (var (stackType, amount) in machineBoard.MaterialRequirements)
            {
                var stack = EntitySystem.Get<StackSystem>().Spawn(amount, stackType, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

                if (!partContainer.Insert(stack))
                    throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {_entMan.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID ?? "N/A"}");
            }

            foreach (var (compName, info) in machineBoard.ComponentRequirements)
            {
                for (var i = 0; i < info.Amount; i++)
                {
                    var c = entityManager.SpawnEntity(info.DefaultPrototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

                    if(!partContainer.Insert(c))
                        throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {_entMan.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID ?? "N/A"}");
                }
            }

            foreach (var (tagName, info) in machineBoard.TagRequirements)
            {
                for (var i = 0; i < info.Amount; i++)
                {
                    var c = entityManager.SpawnEntity(info.DefaultPrototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

                    if(!partContainer.Insert(c))
                        throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {_entMan.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID ?? "N/A"}");
                }
            }
        }

        public void MapInit()
        {
            CreateBoardAndStockParts();
            RefreshParts();
        }
    }
}
