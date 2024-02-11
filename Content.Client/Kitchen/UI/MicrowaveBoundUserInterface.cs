using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Kitchen.UI
{
    [UsedImplicitly]
    public sealed class MicrowaveBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MicrowaveMenu? _menu;

        [ViewVariables]
        private readonly Dictionary<int, EntityUid> _solids = new();

        [ViewVariables]
        private readonly Dictionary<int, ReagentQuantity> _reagents = new();

        private IEntityManager _entManager;

        public MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _entManager = IoCManager.Resolve<IEntityManager>();
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
                SendMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(_solids[args.ItemIndex])));
            };

            _menu.OnCookTimeSelected += (args, buttonIndex) =>
            {
                var actualButton = (MicrowaveMenu.MicrowaveCookTimeButton) args.Button;
                SendMessage(new MicrowaveSelectCookTimeMessage(buttonIndex, actualButton.CookTime));
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

            // TODO move this to a component state and ensure the net ids.
            RefreshContentsDisplay(_entManager.GetEntityArray(cState.ContainedSolids));

            if (_menu == null) return;

            var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex);
            currentlySelectedTimeButton.Pressed = true;
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("microwave-menu-instant-button")
                : cState.CurrentCookTime.ToString();
            _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                         ("time", cookTime));
        }

        private void RefreshContentsDisplay(EntityUid[] containedSolids)
        {
            _reagents.Clear();

            if (_menu == null) return;

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var entity in containedSolids)
            {
                if (EntMan.Deleted(entity))
                {
                    return;
                }

                // TODO just use sprite view

                Texture? texture;
                if (EntMan.TryGetComponent<IconComponent>(entity, out var iconComponent))
                {
                    texture = EntMan.System<SpriteSystem>().GetIcon(iconComponent);
                }
                else if (EntMan.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
                {
                    texture = spriteComponent.Icon?.Default;
                }
                else
                {
                    continue;
                }

                var solidItem = _menu.IngredientsList.AddItem(EntMan.GetComponent<MetaDataComponent>(entity).EntityName, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, entity);
            }
        }
    }
}
