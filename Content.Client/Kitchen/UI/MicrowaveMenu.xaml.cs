using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Kitchen.UI
{
    public class MicrowaveMenu : SS14Window
    {
        public class MicrowaveCookTimeButton : Button
        {
            public uint CookTime;
        }


        private MicrowaveBoundUserInterface Owner { get; set; }

        public event Action<BaseButton.ButtonEventArgs, int>? OnCookTimeSelected;

        public Button StartButton { get; }
        public Button EjectButton { get; }

        public PanelContainer TimerFacePlate { get; }

        public ButtonGroup CookTimeButtonGroup { get; }

        public BoxContainer CookTimeButtonVbox { get; }

        private BoxContainer ButtonGridContainer { get; }

        private PanelContainer DisableCookingPanelOverlay { get; }

        public ItemList IngredientsList { get; }

        public ItemList IngredientsListReagents { get; }
        public Label CookTimeInfoLabel { get; }

        public MicrowaveMenu(MicrowaveBoundUserInterface owner)
        {
            SetSize = MinSize = (512, 256);

            Owner = owner;
            Title = Loc.GetString("microwave-menu-title");
            DisableCookingPanelOverlay = new PanelContainer
            {
                MouseFilter = MouseFilterMode.Stop,
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.60f)},
            };

            var hSplit = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal
            };

            IngredientsListReagents = new ItemList
            {
                VerticalExpand = true,
                HorizontalExpand = true,
                SelectMode = ItemList.ItemListSelectMode.Button,
                SizeFlagsStretchRatio = 2,
                MinSize = (100, 128)
            };

            IngredientsList = new ItemList
            {
                VerticalExpand = true,
                HorizontalExpand = true,
                SelectMode = ItemList.ItemListSelectMode.Button,
                SizeFlagsStretchRatio = 2,
                MinSize = (100, 128)
            };

            hSplit.AddChild(IngredientsListReagents);
            //Padding between the lists.
            hSplit.AddChild(new Control
            {
                MinSize = (0, 5),
            });

            hSplit.AddChild(IngredientsList);

            var vSplit = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                VerticalExpand = true,
                HorizontalExpand = true,
            };

            hSplit.AddChild(vSplit);

            ButtonGridContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Align = BoxContainer.AlignMode.Center,
                SizeFlagsStretchRatio = 3
            };

            StartButton = new Button
            {
                Text = Loc.GetString("microwave-menu-start-button"),
                TextAlign = Label.AlignMode.Center,
            };

            EjectButton = new Button
            {
                Text = Loc.GetString("microwave-menu-eject-all-text"),
                ToolTip = Loc.GetString("microwave-menu-eject-all-tooltip"),
                TextAlign = Label.AlignMode.Center,
            };

            ButtonGridContainer.AddChild(StartButton);
            ButtonGridContainer.AddChild(EjectButton);
            vSplit.AddChild(ButtonGridContainer);

            //Padding
            vSplit.AddChild(new Control
            {
                MinSize = (0, 15),
            });

            CookTimeButtonGroup = new ButtonGroup();
            CookTimeButtonVbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                VerticalExpand = true,
                Align = BoxContainer.AlignMode.Center,
            };

            var index = 0;
            for (var i = 0; i <= 6; i++)
            {
                var newButton = new MicrowaveCookTimeButton
                {
                    Text = index <= 0 ? Loc.GetString("microwave-menu-instant-button") : index.ToString(),
                    CookTime = (uint)index,
                    TextAlign = Label.AlignMode.Center,
                    ToggleMode = true,
                    Group = CookTimeButtonGroup,
                };
                CookTimeButtonVbox.AddChild(newButton);
                newButton.OnToggled += args =>
                {
                    OnCookTimeSelected?.Invoke(args, newButton.GetPositionInParent());

                };
                index += 5;
            }

            var cookTimeOneSecondButton = (Button) CookTimeButtonVbox.GetChild(0);
            cookTimeOneSecondButton.Pressed = true;

            CookTimeInfoLabel = new Label
            {
                Text = Loc.GetString("microwave-menu-cook-time-label", ("time", 1)), // TODO, hardcoded value
                Align = Label.AlignMode.Center,
                Modulate = Color.White,
                VerticalAlignment = VAlignment.Center
            };

            var innerTimerPanel = new PanelContainer
            {
                VerticalExpand = true,
                ModulateSelfOverride = Color.Red,
                MinSize = (100, 128),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.5f)},

                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        Children =
                        {
                            new PanelContainer
                            {
                                PanelOverride = new StyleBoxFlat() {BackgroundColor = Color.Gray.WithAlpha(0.2f)},

                                Children =
                                {
                                    CookTimeInfoLabel
                                }
                            },

                            new ScrollContainer()
                            {
                                VerticalExpand = true,

                                Children =
                                {
                                    CookTimeButtonVbox,
                                }
                            },
                        }
                    }
                }
            };

            TimerFacePlate = new PanelContainer()
            {
                VerticalExpand = true,
                HorizontalExpand = true,
                Children =
                {
                    innerTimerPanel
                },
            };

            vSplit.AddChild(TimerFacePlate);
            Contents.AddChild(hSplit);
            Contents.AddChild(DisableCookingPanelOverlay);
        }

        public void ToggleBusyDisableOverlayPanel(bool shouldDisable)
        {
            DisableCookingPanelOverlay.Visible = shouldDisable;
        }
    }
}
