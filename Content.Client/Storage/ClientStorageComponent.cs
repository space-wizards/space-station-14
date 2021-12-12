using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Animations;
using Content.Client.Items.Managers;
using Content.Client.Storage.UI;
using Content.Shared.DragDrop;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Client.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    public class ClientStorageComponent : SharedStorageComponent, IDraggable
    {
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private List<EntityUid> _storedEntities = new();
        private int StorageSizeUsed;
        private int StorageCapacityMax;
        private StorageWindow? _window;
        public bool UIOpen => _window?.IsOpen ?? false;

        public override IReadOnlyList<EntityUid> StoredEntities => _storedEntities;

        protected override void Initialize()
        {
            base.Initialize();

            // Hide stackVisualizer on start
            ChangeStorageVisualization(SharedBagState.Closed);
        }

        protected override void OnAdd()
        {
            base.OnAdd();
            _window = new StorageWindow(OnInteract, OnInsert) {Title = _entityManager .GetComponent<MetaDataComponent>(Owner).EntityName};
#pragma warning disable 618
            _window.OnClose += () => SendNetworkMessage(new CloseStorageUIMessage());
#pragma warning restore 618
        }

        protected override void OnRemove()
        {
            _window?.Dispose();
            base.OnRemove();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not StorageComponentState state)
            {
                return;
            }

            _storedEntities = state.StoredEntities.ToList();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                //Updates what we are storing for the UI
                case StorageHeldItemsMessage msg:
                    HandleStorageMessage(msg);
                    break;
                //Opens the UI
                case OpenStorageUIMessage _:
                    ToggleUI();
                    break;
                case CloseStorageUIMessage _:
                    CloseUI();
                    break;
                case AnimateInsertingEntitiesMessage msg:
                    HandleAnimatingInsertingEntities(msg);
                    break;
            }
        }

        /// <summary>
        /// Copies received values from server about contents of storage container
        /// </summary>
        /// <param name="storageState"></param>
        private void HandleStorageMessage(StorageHeldItemsMessage storageState)
        {
            _storedEntities = storageState.StoredEntities.ToList();
            StorageSizeUsed = storageState.StorageSizeUsed;
            StorageCapacityMax = storageState.StorageSizeMax;
            _window?.BuildEntityList(_storedEntities, StorageSizeUsed, StorageCapacityMax);
        }

        /// <summary>
        /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
        /// </summary>
        /// <param name="msg"></param>
        private void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesMessage msg)
        {
            for (var i = 0; msg.StoredEntities.Count > i; i++)
            {
                var entity = msg.StoredEntities[i];
                var initialPosition = msg.EntityPositions[i];

                ReusableAnimations.AnimateEntityPickup(entity, initialPosition,
                    _entityManager .GetComponent<TransformComponent>(Owner).LocalPosition, _entityManager );
            }
        }

        /// <summary>
        /// Opens the storage UI if closed. Closes it if opened.
        /// </summary>
        private void ToggleUI()
        {
            if (_window == null) return;

            if (_window.IsOpen)
            {
                _window.Close();
                ChangeStorageVisualization(SharedBagState.Closed);
            }
            else
            {
                _window.OpenCentered();
                ChangeStorageVisualization(SharedBagState.Open);
            }
        }

        private void CloseUI()
        {
            if (_window == null) return;

            _window.Close();
            ChangeStorageVisualization(SharedBagState.Closed);

        }

        private void ChangeStorageVisualization(SharedBagState state)
        {
            if (_entityManager .TryGetComponent<AppearanceComponent>(Owner, out var appearanceComponent))
            {
                appearanceComponent.SetData(SharedBagOpenVisuals.BagState, state);
                if (_entityManager .HasComponent<ItemCounterComponent>(Owner))
                {
                    appearanceComponent.SetData(StackVisuals.Hide, state == SharedBagState.Closed);
                }
            }
        }

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entity"></param>
        private void OnInteract(BaseButton.ButtonEventArgs buttonEventArgs, EntityUid entity)
        {
            if (buttonEventArgs.Event.Function == EngineKeyFunctions.UIClick)
            {
#pragma warning disable 618
                SendNetworkMessage(new RemoveEntityMessage(entity));
#pragma warning restore 618
                buttonEventArgs.Event.Handle();
            }
            _itemSlotManager.OnButtonPressed(buttonEventArgs.Event, entity);
        }

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entity"></param>
        private void OnInsert(BaseButton.ButtonEventArgs buttonEventArgs)
        {
#pragma warning disable 618
            SendNetworkMessage(new InsertEntityMessage());
#pragma warning restore 618
        }

        public override bool Remove(EntityUid entity)
        {
            if (_storedEntities.Remove(entity))
            {
                Dirty();
                return true;
            }

            return false;
        }
    }
}
