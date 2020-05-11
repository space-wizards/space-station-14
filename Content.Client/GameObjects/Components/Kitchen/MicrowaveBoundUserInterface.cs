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
        private Dictionary<int, Solution.ReagentQuantity> _reagents =new Dictionary<int, Solution.ReagentQuantity>();

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
            _menu.IngredientsList.OnItemSelected += args =>
            { 
                SendMessage(new SharedMicrowaveComponent.MicrowaveEjectSolidIndexedMessage(_solids[args.ItemIndex]));
                
            };

            _menu.IngredientsListReagents.OnItemSelected += args =>
            {
                SendMessage(
                    new SharedMicrowaveComponent.MicrowaveVaporizeReagentIndexedMessage(_reagents[args.ItemIndex]));
            };
                
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


        private void RefreshContentsDisplay(IReadOnlyList<Solution.ReagentQuantity> reagents, List<EntityUid> solids)
        {
            _reagents.Clear();
            _menu.IngredientsListReagents.Clear();
            foreach (var reagent in reagents)
            {
                _prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto);
                var reagentAdded = _menu.IngredientsListReagents.AddItem($"{reagent.Quantity} {proto.Name}");
                var reagentIndex = _menu.IngredientsListReagents.IndexOf(reagentAdded);
                _reagents.Add(reagentIndex, reagent);
            }

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var entityID in solids)
            {
                var entity = _entityManager.GetEntity(entityID);

                if (entity.TryGetComponent(out IconComponent icon))
                {
                    var solidItem = _menu.IngredientsList.AddItem(entity.Name, icon.Icon.Default);

                    var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                    _solids.Add(solidIndex, entityID);
                }


            }

        }
    }
}
