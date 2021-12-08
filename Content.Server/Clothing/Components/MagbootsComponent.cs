using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.Actions;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public sealed class MagbootsComponent : SharedMagbootsComponent, IUnequipped, IEquipped, IUse, IActivate
    {
        [ComponentDependency] private ItemComponent? _item = null;
        [ComponentDependency] private ItemActionsComponent? _itemActions = null;
        [ComponentDependency] private SpriteComponent? _sprite = null;

        [Dependency] private readonly IEntityManager _entMan = default!;

        private bool _on;

        [ViewVariables]
        public override bool On
        {
            get => _on;
            set
            {
                _on = value;

                UpdateContainer();
                _itemActions?.Toggle(ItemActionType.ToggleMagboots, On);
                if (_item != null)
                    _item.EquippedPrefix = On ? "on" : null;
                _sprite?.LayerSetState(0, On ? "icon-on" : "icon");
                OnChanged();
                Dirty();
            }
        }

        public void Toggle(EntityUid user)
        {
            On = !On;
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            if (On && eventArgs.Slot == Slots.SHOES)
            {
                if (_entMan.TryGetComponent(eventArgs.User, out MovedByPressureComponent? movedByPressure))
                {
                    movedByPressure.Enabled = true;
                }

                if (_entMan.TryGetComponent(eventArgs.User, out ServerAlertsComponent? alerts))
                {
                    alerts.ClearAlert(AlertType.Magboots);
                }
            }
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            UpdateContainer();
        }

        private void UpdateContainer()
        {
            if (!Owner.TryGetContainer(out var container))
                return;

            if (_entMan.TryGetComponent(container.Owner, out InventoryComponent? inventoryComponent)
                && inventoryComponent.GetSlotItem(Slots.SHOES)?.Owner == Owner)
            {
                if (_entMan.TryGetComponent(container.Owner, out MovedByPressureComponent? movedByPressure))
                {
                    movedByPressure.Enabled = false;
                }

                if (_entMan.TryGetComponent(container.Owner, out ServerAlertsComponent? alerts))
                {
                    if (On)
                    {
                        alerts.ShowAlert(AlertType.Magboots);
                    }
                    else
                    {
                        alerts.ClearAlert(AlertType.Magboots);
                    }
                }
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
        }

        public override ComponentState GetComponentState()
        {
            return new MagbootsComponentState(On);
        }
    }

    [UsedImplicitly]
    [DataDefinition]
    public sealed class ToggleMagbootsAction : IToggleItemAction
    {
        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<MagbootsComponent?>(args.Item, out var magboots))
                return false;

            magboots.Toggle(args.Performer);
            return true;
        }
    }
}
