using System;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Computer
{
    [RegisterComponent]
    public sealed class ComputerComponent : SharedComputerComponent, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        [DataField("board", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _boardPrototype;

        protected override void Initialize()
        {
            base.Initialize();

            // Let's ensure the container manager and container are here.
            Owner.EnsureContainer<Container>("board", out var _);

            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? powerReceiver) &&
                _entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(ComputerVisuals.Powered, powerReceiver.Powered);
            }
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerReceiverOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        private void PowerReceiverOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(ComputerVisuals.Powered, e.Powered);
            }
        }

        /// <summary>
        ///     Creates the corresponding computer board on the computer.
        ///     This exists so when you deconstruct computers that were serialized with the map,
        ///     you can retrieve the computer board.
        /// </summary>
        private void CreateComputerBoard()
        {
            // Ensure that the construction component is aware of the board container.
            if (_entMan.TryGetComponent(Owner, out ConstructionComponent? construction))
                EntitySystem.Get<ConstructionSystem>().AddContainer(Owner, "board", construction);

            // We don't do anything if this is null or empty.
            if (string.IsNullOrEmpty(_boardPrototype))
                return;

            var container = Owner.EnsureContainer<Container>("board", out var existed);

            if (existed)
            {
                // We already contain a board. Note: We don't check if it's the right one!
                if (container.ContainedEntities.Count != 0)
                    return;
            }

            var board = _entMan.SpawnEntity(_boardPrototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

            if(!container.Insert(board))
                Logger.Warning($"Couldn't insert board {board} to computer {Owner}!");
        }

        public void MapInit()
        {
            CreateComputerBoard();
        }
    }
}
