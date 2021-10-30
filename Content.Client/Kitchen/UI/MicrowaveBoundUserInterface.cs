using System.Collections.Generic;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Content.Shared.Kitchen.Components.SharedMicrowaveComponent;

namespace Content.Client.Kitchen.UI
{
    [UsedImplicitly]
    public class MicrowaveBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private MicrowaveMenu? _menu;

        private readonly Dictionary<int, EntityUid> _solids = new();
        private readonly Dictionary<int, Solution.ReagentQuantity> _reagents =new();

        public MicrowaveBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner,uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = new MicrowaveMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.StartButton.OnPressed += _ => SendMessage(new MicrowaveStartCookMessage());
            _menu.EjectButton.OnPressed += _ => SendMessage(new MicrowaveEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendMessage(new MicrowaveEjectSolidIndexedMessage(_solids[args.ItemIndex]));
            };

            _menu.IngredientsListReagents.OnItemSelected += args =>
            {
                SendMessage(new MicrowaveVaporizeReagentIndexedMessage(_reagents[args.ItemIndex]));
            };

            _menu.OnCookTimeSelected += (args,buttonIndex) =>
            {
                var actualButton = (MicrowaveMenu.MicrowaveCookTimeButton) args.Button ;
                SendMessage(new MicrowaveSelectCookTimeMessage(buttonIndex,actualButton.CookTime));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _solids.Clear();
            _menu?.Dispose();
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not MicrowaveUpdateUserInterfaceState cState)
            {
                return;
            }

            _menu?.ToggleBusyDisableOverlayPanel(cState.IsMicrowaveBusy);
            RefreshContentsDisplay(cState.ReagentQuantities, cState.ContainedSolids);

            if (_menu == null) return;

            var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex);
            currentlySelectedTimeButton.Pressed = true;
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("microwave-menu-instant-button")
                : cState.CurrentCookTime.ToString();
            _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                         ("time", cookTime));
        }

        private void RefreshContentsDisplay(Solution.ReagentQuantity[] reagents, EntityUid[] containedSolids)
        {
            _reagents.Clear();

            if (_menu == null) return;

            _menu.IngredientsListReagents.Clear();
            for (var i = 0; i < reagents.Length; i++)
            {
                if (!_prototypeManager.TryIndex(reagents[i].ReagentId, out ReagentPrototype? proto)) continue;

                var reagentAdded = _menu.IngredientsListReagents.AddItem($"{reagents[i].Quantity} {proto.Name}");
                var reagentIndex = _menu.IngredientsListReagents.IndexOf(reagentAdded);
                _reagents.Add(reagentIndex, reagents[i]);
            }

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var t in containedSolids)
            {
                if (!_entityManager.TryGetEntity(t, out var entity))
                {
                    return;
                }

                if (entity.Deleted)
                {
                    continue;
                }

                Texture? texture;
                if (entity.TryGetComponent(out IconComponent? iconComponent))
                {
                    texture = iconComponent.Icon?.Default;
                }
                else if (entity.TryGetComponent(out SpriteComponent? spriteComponent))
                {
                    texture = spriteComponent.Icon?.Default;
                }
                else
                {
                    continue;
                }

                var solidItem = _menu.IngredientsList.AddItem(entity.Name, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, t);
            }
        }
    }
}
