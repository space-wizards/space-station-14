using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Server.Interfaces.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineComponent : Component, IMapInit
    {
        public override string Name => "Machine";

        [DataField("board")]
        public string BoardPrototype { get; private set; }

        private Container _boardContainer;
        private Container _partContainer;

        public override void Initialize()
        {
            base.Initialize();

            _boardContainer = ContainerHelpers.EnsureContainer<Container>(Owner, MachineFrameComponent.BoardContainer);
            _partContainer = ContainerHelpers.EnsureContainer<Container>(Owner, MachineFrameComponent.PartContainer);
        }

        protected override void Startup()
        {
            base.Startup();

            CreateBoardAndStockParts();
        }

        public IEnumerable<MachinePartComponent> GetAllParts()
        {
            foreach (var entity in _partContainer.ContainedEntities)
            {
                if (entity.TryGetComponent<MachinePartComponent>(out var machinePart))
                    yield return machinePart;
            }
        }

        public void RefreshParts()
        {
            foreach (var refreshable in Owner.GetAllComponents<IRefreshParts>())
            {
                refreshable.RefreshParts(GetAllParts());
            }
        }

        public void CreateBoardAndStockParts()
        {
            // Entity might not be initialized yet.
            var boardContainer = ContainerHelpers.EnsureContainer<Container>(Owner, MachineFrameComponent.BoardContainer, out var existedBoard);
            var partContainer = ContainerHelpers.EnsureContainer<Container>(Owner, MachineFrameComponent.PartContainer, out var existedParts);

            if (string.IsNullOrEmpty(BoardPrototype))
                return;

            var entityManager = Owner.EntityManager;

            if (existedBoard || existedParts)
            {
                // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
                if (boardContainer.ContainedEntities.Count > 0)
                    return;
            }

            var board = entityManager.SpawnEntity(BoardPrototype, Owner.Transform.Coordinates);

            if (!_boardContainer.Insert(board))
            {
                throw new Exception($"Couldn't insert board with prototype {BoardPrototype} to machine with prototype {Owner.Prototype?.ID ?? "N/A"}!");
            }

            if (!board.TryGetComponent<MachineBoardComponent>(out var machineBoard))
            {
                throw new Exception($"Entity with prototype {BoardPrototype} doesn't have a {nameof(MachineBoardComponent)}!");
            }

            foreach (var (part, amount) in machineBoard.Requirements)
            {
                for (var i = 0; i < amount; i++)
                {
                    var p = entityManager.SpawnEntity(MachinePartComponent.Prototypes[part], Owner.Transform.Coordinates);

                    if (!partContainer.Insert(p))
                        throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {Owner.Prototype?.ID ?? "N/A"}!");
                }
            }

            foreach (var (stackType, amount) in machineBoard.MaterialRequirements)
            {
                var s = StackHelpers.SpawnStack(stackType, amount, Owner.Transform.Coordinates);

                if (!partContainer.Insert(s))
                    throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Owner.Prototype?.ID ?? "N/A"}");
            }

            foreach (var (compName, info) in machineBoard.ComponentRequirements)
            {
                for (var i = 0; i < info.Amount; i++)
                {
                    var c = entityManager.SpawnEntity(info.DefaultPrototype, Owner.Transform.Coordinates);

                    if(!partContainer.Insert(c))
                        throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {Owner.Prototype?.ID ?? "N/A"}");
                }
            }

            foreach (var (tagName, info) in machineBoard.TagRequirements)
            {
                for (var i = 0; i < info.Amount; i++)
                {
                    var c = entityManager.SpawnEntity(info.DefaultPrototype, Owner.Transform.Coordinates);

                    if(!partContainer.Insert(c))
                        throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {Owner.Prototype?.ID ?? "N/A"}");
                }
            }
        }

        public void MapInit()
        {
            CreateBoardAndStockParts();
        }
    }
}
