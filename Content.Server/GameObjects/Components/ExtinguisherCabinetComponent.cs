using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{

    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ExtinguisherCabinetComponent : Component, IInteractUsing, IInteractHand, IActivate
    {

        // TODO
        // - Deconstruct with wrench
        //     - "You start unsecuring [name]..." text
        //     - deconstruct.ogg
        //         - 50 volume
        //     - Spawn 2 metal
        //     - Spawn extinguisher
        // - Build with 2 metal
        //     - Do not spawn with extinguisher
        // - Map into Saltern
        // - Test in multiplayer

        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public override string Name => "ExtinguisherCabinet";

        [ViewVariables] private ContainerSlot _itemContainer;
        private bool _opened = false;

        public override void Initialize()
        {
            base.Initialize();

            _itemContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("extinguisher_cabinet", Owner, out _);

            _itemContainer.Insert(Owner.EntityManager.SpawnEntity("FireExtinguisher", Owner.Transform.GridPosition));
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_itemContainer.ContainedEntity != null || !eventArgs.Using.HasComponent<FireExtinguisherComponent>())
            {
                return false;
            }
            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.Using, _itemContainer))
            {
                return false;
            }

            UpdateVisuals();

            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (_opened)
            {
                if (_itemContainer.ContainedEntity == null)
                {
                    _opened = false;
                    ClickLatchSound();
                }
                else if (eventArgs.User.TryGetComponent(out HandsComponent hands))
                {
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                        Loc.GetString("You take {0:extinguisherName} from the {1:cabinetName}", _itemContainer.ContainedEntity.Name, Owner.Name));
                    hands.PutInHandOrDrop(_itemContainer.ContainedEntity.GetComponent<ItemComponent>());
                }
                else if (_itemContainer.Remove(_itemContainer.ContainedEntity))
                {
                    _itemContainer.ContainedEntity.Transform.GridPosition = Owner.Transform.GridPosition;
                }
            }
            else
            {
                _opened = true;
                ClickLatchSound();
            }

            UpdateVisuals();

            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            _opened = !_opened;
            ClickLatchSound();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(ExtinguisherCabinetVisuals.IsOpen, _opened);
                appearance.SetData(ExtinguisherCabinetVisuals.ContainsExtinguisher, _itemContainer.ContainedEntity != null);
            }
        }

        private void ClickLatchSound()
        {
            EntitySystem.Get<AudioSystem>() // Don't have original click, this sounds close
                .PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioHelpers.WithVariation(0.15f));
        }
    }
}
