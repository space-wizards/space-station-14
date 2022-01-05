using Content.Shared.Actions;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedMagbootsComponent))]
    public sealed class MagbootsComponent : SharedMagbootsComponent, IActivate
    {
        [ComponentDependency] private SharedItemComponent? _item = null;
        [ComponentDependency] private ItemActionsComponent? _itemActions = null;
        [ComponentDependency] private SpriteComponent? _sprite = null;

        private bool _on;

        [ViewVariables]
        public override bool On
        {
            get => _on;
            set
            {
                _on = value;

                if (Owner.TryGetContainer(out var container) && EntitySystem.Get<InventorySystem>()
                        .TryGetSlotEntity(container.Owner, "shoes", out var entityUid) && entityUid == Owner)
                {
                    EntitySystem.Get<MagbootsSystem>().UpdateMagbootEffects(container.Owner, Owner, true, this);
                }

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
