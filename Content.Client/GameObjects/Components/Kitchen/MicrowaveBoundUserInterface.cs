using Robust.Client.GameObjects.Components.UserInterface;
using Content.Shared.Kitchen;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;


using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public  class MicrowaveBoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649
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
            _menu.IngredientsList.OnItemSelected += args => SendMessage(new SharedMicrowaveComponent.MicrowaveEjectSolidIndexedMessage(_solids[args.ItemIndex]));
            _menu.OnCookTimeSelected += args =>
            {
                var actualButton = args.Button as Button;
                var newTime = (uint) int.Parse(actualButton.Text);
                _menu.VisualCookTime = newTime;
                SendMessage(new SharedMicrowaveComponent.MicrowaveSelectCookTimeMessage(newTime));
            };

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


        private void RefreshContentsDisplay(List<Solution.ReagentQuantity> reagents, List<EntityUid> solids)
        {
            _menu.IngredientsList.Clear();
            foreach (var item in reagents)
            {
                _prototypeManager.TryIndex(item.ReagentId, out ReagentPrototype proto);

                _menu.IngredientsList.AddItem($"{item.Quantity} {proto.Name}");
            }

            _solids.Clear();
            foreach (var entityID in solids)
            {
                var entity = _entityManager.GetEntity(entityID);

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
