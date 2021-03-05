using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public sealed class ComputerComponent : SharedComputerComponent, IMapInit
    {
        [ViewVariables]
        [DataField("board")]
        private string _boardPrototype;

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out PowerReceiverComponent powerReceiver))
            {
                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ComputerVisuals.Powered, powerReceiver.Powered);
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();

            CreateComputerBoard();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerReceiverOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        private void PowerReceiverOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
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
            // We don't do anything if this is null or empty.
            if (string.IsNullOrEmpty(_boardPrototype))
                return;

            var container = ContainerHelpers.EnsureContainer<Container>(Owner, "board", out var existed);

            if (existed)
            {
                // We already contain a board. Note: We don't check if it's the right one!
                if (container.ContainedEntities.Count != 0)
                    return;
            }

            var board = Owner.EntityManager.SpawnEntity(_boardPrototype, Owner.Transform.Coordinates);

            if(!container.Insert(board))
                Logger.Warning($"Couldn't insert board {board} to computer {Owner}!");

            if (Owner.TryGetComponent(out ConstructionComponent construction))
                construction.AddContainer("board");
        }

        public void MapInit()
        {
            CreateComputerBoard();
        }
    }
}
