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
        [Dependency] private readonly IEntityManager _entMan = default!;

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

                if(_entMan.TryGetComponent<ItemActionsComponent>(Owner, out var itemActions))
                    itemActions.Toggle(ItemActionType.ToggleMagboots, On);
                if (_entMan.TryGetComponent<SharedItemComponent>(Owner, out var item))
                    item.EquippedPrefix = On ? "on" : null;
                if(_entMan.TryGetComponent<SpriteComponent>(Owner, out var sprite))
                    sprite.LayerSetState(0, On ? "icon-on" : "icon");
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
