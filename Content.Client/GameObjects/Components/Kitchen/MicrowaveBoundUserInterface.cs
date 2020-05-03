using Robust.Client.GameObjects.Components.UserInterface;
using Content.Shared.Kitchen;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.GameObjects;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public  class MicrowaveBoundUserInterface : BoundUserInterface
    {
        private MicrowaveMenu _menu;

        private Dictionary<int, EntityUid> _solids = new Dictionary<int, EntityUid>();

        public MicrowaveBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner,uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();
            _menu = new MicrowaveMenu(this);

            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.StartButton.OnPressed += args => SendMessage(new SharedMicrowaveComponent.MicrowaveStartCookMessage());
            _menu.EjectButton.OnPressed += args => SendMessage(new SharedMicrowaveComponent.MicrowaveEjectMessage());
            _menu.IngredientsList.OnItemSelected += args => EjectSolidWithIndex(args.ItemIndex);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }
            _solids?.Clear();
            _menu?.Dispose();
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is MicrowaveUpdateUserInterfaceState cstate))
            {
                return;
            }

            RefreshContentsDisplay(cstate.ReagentsReagents, cstate.ContainedSolids);

        }


        public void EjectSolidWithIndex(int index)
        {
            SendMessage(new SharedMicrowaveComponent.MicrowaveEjectSolidIndexedMessage(_solids[index]));
        }

        public void RefreshContentsDisplay(List<Solution.ReagentQuantity> reagents, List<EntityUid> solids)
        {
            _menu.IngredientsList.Clear();
            foreach (var item in reagents)
            {
                IoCManager.Resolve<IPrototypeManager>().TryIndex(item.ReagentId, out ReagentPrototype proto);

                _menu.IngredientsList.AddItem($"{item.Quantity} {proto.Name}");
            }

            _solids.Clear();
            foreach (var entityID in solids)
            {
                var entity = IoCManager.Resolve<IEntityManager>().GetEntity(entityID);

                if (entity.TryGetComponent(out IconComponent icon))
                {
                    var itemItem = _menu.IngredientsList.AddItem(entity.Name, icon.Icon.Default);

                    var index = _menu.IngredientsList.IndexOf(itemItem);
                    _solids.Add(index, entityID);
                }

                
            }

        }
    }
}
