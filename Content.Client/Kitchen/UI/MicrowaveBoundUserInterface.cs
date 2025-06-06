using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Kitchen.UI
{
    [UsedImplicitly]
    public sealed class MicrowaveBoundUserInterface : BoundUserInterface
    {
        // Starlight-start
        private IEntityManager _entManager;
        
        private IGameTiming _timing = default!;
        
        [ViewVariables]
        private EntityUid? _owner;
        // Starlight-end
        
        [ViewVariables]
        private MicrowaveMenu? _menu;

        [ViewVariables]
        private readonly Dictionary<int, EntityUid> _solids = new();

        [ViewVariables]
        private readonly Dictionary<int, ReagentQuantity> _reagents = new();

        public MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            // Starlight-start
            _owner = owner;
            _entManager = IoCManager.Resolve<IEntityManager>();
            _timing = IoCManager.Resolve<IGameTiming>();
            // Starlight-end
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<MicrowaveMenu>();
            
            // Starlight-start
            if (!_entManager.TryGetComponent<CookingDeviceComponent>(_owner, out var cookingDevice))
                return;
            
            if (_owner != null && cookingDevice.Safe)
                _menu.AddCookingButtons();
            else
            {
                _menu.CookTimeInfoLabel.Text = "";
                _menu.InstantCookButton.Visible = false;
            }
            
            _menu.StopButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStopCookMessage());
            _menu.StopButton.Visible = false;
            
            // Starlight-end
            
            _menu.StartButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStartCookMessage());
            _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new MicrowaveEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(_solids[args.ItemIndex])));
            };

            _menu.OnCookTimeSelected += (args, buttonIndex) =>
            {
                var selectedCookTime = (uint) 0;

                if (args.Button is MicrowaveMenu.MicrowaveCookTimeButton microwaveCookTimeButton)
                {
                    // args.Button is a MicrowaveCookTimeButton
                    var actualButton = (MicrowaveMenu.MicrowaveCookTimeButton) args.Button;
                    selectedCookTime = actualButton.CookTime == 0 ? 0 : actualButton.CookTime;
                    // SendMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime / 5, actualButton.CookTime));
                    SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime / 5, actualButton.CookTime));

                    // Starlight-start
                    if (cookingDevice.Safe)
                        _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label", ("time", selectedCookTime));
                    // Starlight-end
                }
                else
                {
                    // args.Button is a normal button aka instant cook button
                    SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime, 0));
                    
                    // Starlight-start
                    if (cookingDevice.Safe)
                        _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label", ("time", Loc.GetString("microwave-menu-instant-button")));
                    // Starlight-end
                }
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not MicrowaveUpdateUserInterfaceState cState || _menu == null)
            {
                return;
            }

            _menu.IsBusy = cState.IsMicrowaveBusy;
            _menu.IsSafe = cState.IsMicrowaveSafe; // Starlight-edit
            _menu.CurrentCooktimeEnd = cState.CurrentCookTimeEnd;

            _menu.ToggleBusyDisableOverlayPanel(cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0);
            // TODO move this to a component state and ensure the net ids.
            RefreshContentsDisplay(EntMan.GetEntityArray(cState.ContainedSolids));

            //Set the cook time info label
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("microwave-menu-instant-button")
                : cState.CurrentCookTime.ToString();
            
            // Starlight-start
            if (cState.IsMicrowaveSafe)
                _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label", ("time", cookTime));
            else
            {
                _menu.CookTimeInfoLabel.Text = "";
                _menu.InstantCookButton.Visible = false;
            }
            // Starlight-end
            _menu.StartButton.Disabled = cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0;
            _menu.StartButton.Visible = !cState.IsMicrowaveBusy; // Starlight-edit
            _menu.StopButton.Visible = cState.IsMicrowaveBusy; // Starlight-edit
            _menu.StopButton.Disabled = !cState.IsMicrowaveBusy; // Starlight-edit
            _menu.EjectButton.Disabled = cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0;
            
            // Starlight-start
            
            if (cState.IsMicrowaveSafe)
                _menu.AddCookingButtons();
            else
                _menu.ClearCookingButtons();
            
            _menu.StartedCooktime = cState.StartedCookTime;
            
            if (cState.StartedCookTime != TimeSpan.Zero)
                _menu.CurrentCookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-current-cook-time-label", ("time", (_timing.CurTime - cState.StartedCookTime).ToString(@"mm\:ss")));
            else
                _menu.CurrentCookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-current-cook-time-label", ("time", cState.StartedCookTime.ToString(@"mm\:ss")));
            
            // Starlight-end


            //Set the correct button active button
            if (cState.ActiveButtonIndex == 0)
            {
                _menu.InstantCookButton.Pressed = true;
            }
            else
            {
                var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex - 1);
                currentlySelectedTimeButton.Pressed = true;
            }
            
            // Starlight-start
            foreach (Button children in _menu.CookTimeButtonVbox.Children)
            {
                children.Disabled = cState.IsMicrowaveBusy;
            }
            
            _menu.InstantCookButton.Disabled = cState.IsMicrowaveBusy;
            
            // Starlight-end

            //Set the "micowave light" ui color to indicate if the microwave is busy or not
            if (cState.IsMicrowaveBusy && cState.ContainedSolids.Length > 0)
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#947300") };
            }
            else
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#1B1B1E") };
            }
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
