using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{

    [RegisterComponent]
    public class ExtinguisherCabinetComponent : Component, IInteractUsing, IInteractHand
    {

        // TODO
        // - Spawn with extinguisher in init
        // - Alt click to close
        //     - Extra inspect text
        // - Deconstruct with wrench
        //     - "You start unsecuring [name]..." text
        //     - deconstruct.ogg
        //         - 50 volume
        //     - Spawn 2 metal
        //     - Spawn extinguisher
        // - Build with 2 metal
        //     - Do not spawn with extinguisher
        // - Place extinguisher inside if opened
        // - Click to open => Click to get extinguisher
        //     - You take [stored_extinguisher] from [src].
        //     - sound/machines/click.ogg
        //         - 15 volume
        // - Test in multiplayer

        public override string Name => "ExtinguisherCabinet";

        [ViewVariables] private ContainerSlot _itemContainer;
        private bool _opened = false;

        public override void Initialize()
        {
            base.Initialize();

            _itemContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("extinguisher_cabinet", Owner, out _);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // if (_itemContainer.ContainedEntity != null)
            // {
            //     Rustle();
            //
            //     Owner.PopupMessage(eventArgs.User, Loc.GetString("There's already something in here?!"));
            //     return false;
            // }

            // var size = eventArgs.Using.GetComponent<ItemComponent>().ObjectSize;

            // TODO: use proper text macro system for this.

            // if (size > MaxItemSize)
            // {
            //     Owner.PopupMessage(eventArgs.User,
            //         Loc.GetString("{0:TheName} is too big to fit in the plant!", eventArgs.Using));
            //     return false;
            // }

            // var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();
            //
            // if (!handsComponent.Drop(eventArgs.Using, _itemContainer))
            // {
            //     return false;
            // }
            //
            // Owner.PopupMessage(eventArgs.User, Loc.GetString("You hide {0:theName} in the plant.", eventArgs.Using));
            // Rustle();
            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            // Rustle();
            //
            // if (_itemContainer.ContainedEntity == null)
            // {
            //     Owner.PopupMessage(eventArgs.User, Loc.GetString("You root around in the roots."));
            //     return true;
            // }
            //
            // Owner.PopupMessage(eventArgs.User, Loc.GetString("There was something in there!"));
            // if (eventArgs.User.TryGetComponent(out HandsComponent hands))
            // {
            //     hands.PutInHandOrDrop(_itemContainer.ContainedEntity.GetComponent<ItemComponent>());
            // }
            // else if (_itemContainer.Remove(_itemContainer.ContainedEntity))
            // {
            //     _itemContainer.ContainedEntity.Transform.GridPosition = Owner.Transform.GridPosition;
            // }

            return true;
        }

        private void Rustle()
        {
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/Effects/plant_rustle.ogg", Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
