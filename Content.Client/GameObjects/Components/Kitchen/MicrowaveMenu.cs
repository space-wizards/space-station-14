using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public class MicrowaveMenu : SS14Window
    {
        protected override Vector2? CustomSize => (512, 256);

        private MicrowaveBoundUserInterface Owner { get; set; }

        public event Action<BaseButton.ButtonEventArgs> OnCookTimeSelected;

        public uint VisualCookTime = 1;

        public Button StartButton { get;}
        public Button EjectButton { get;}

        public PanelContainer TimerFacePlate { get; }

        public ButtonGroup CookTimeButtonGroup { get; }
        private VBoxContainer CookTimeButtonVbox { get; }

        public ItemList IngredientsList { get;}
        private Label _cookTimeInfoLabel { get; }

        public MicrowaveMenu(MicrowaveBoundUserInterface owner = null)
        {
            Owner = owner;
            Title = Loc.GetString("Microwave");
            var hSplit = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Fill
            };


            IngredientsList = new ItemList
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SelectMode = ItemList.ItemListSelectMode.Button,
                SizeFlagsStretchRatio = 8,
                CustomMinimumSize = (200,256)
            };

            hSplit.AddChild(IngredientsList);

            var vSplit = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            hSplit.AddChild(vSplit);

            var buttonGridContainer = new VBoxContainer
            {
                Align  = BoxContainer.AlignMode.Center,
                SizeFlagsStretchRatio = 3
            };

            StartButton = new Button
            {
                Text = Loc.GetString("Start"),
                TextAlign = Label.AlignMode.Center,

            };

            EjectButton = new Button
            {
                Text = Loc.GetString("Eject All Contents"),
                ToolTip = Loc.GetString("This vaporizes all reagents, but ejects any solids."),
                TextAlign = Label.AlignMode.Center,
            };

            buttonGridContainer.AddChild(StartButton);
            buttonGridContainer.AddChild(EjectButton);
            vSplit.AddChild(buttonGridContainer);
            
            //Padding
            vSplit.AddChild(new Control
            {
                CustomMinimumSize = (0, 15),
                SizeFlagsVertical = SizeFlags.Fill,
            });

            CookTimeButtonGroup = new ButtonGroup();
            CookTimeButtonVbox = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                Align = BoxContainer.AlignMode.Center,
            };

            var index = 0;
            for (var i = 0; i <= 12; i++)
            {
                var newButton = new Button
                {
                    Text = (index <= 0 ? 1 : index).ToString(),
                    TextAlign = Label.AlignMode.Center,
                    Group =  CookTimeButtonGroup,
                };
                CookTimeButtonVbox.AddChild(newButton);
                newButton.OnPressed += args =>
                {
                    OnCookTimeSelected?.Invoke(args);
                    _cookTimeInfoLabel.Text = $"{Loc.GetString("COOK TIME")}: {VisualCookTime}";
                };
                index+=5;
            }

            var cookTimeOneSecondButton = (Button)CookTimeButtonVbox.GetChild(0);
            cookTimeOneSecondButton.Pressed = true;
            
            _cookTimeInfoLabel = new Label
            {
                Text = Loc.GetString($"COOK TIME: {VisualCookTime}"),
                Align = Label.AlignMode.Center,
                Modulate = Color.White,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            var innerTimerPanel = new PanelContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                ModulateSelfOverride = Color.Red,
                CustomMinimumSize = (100, 128),
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.5f)},

                Children =
                {

                    new VBoxContainer
                    {

                        Children =
                        {
  
                            new PanelContainer
                            {
                                PanelOverride = new StyleBoxFlat(){BackgroundColor = Color.Gray.WithAlpha(0.2f)},

                                Children =
                                {
                                    _cookTimeInfoLabel
                                }
                            },

                            new ScrollContainer()
                            {
                                SizeFlagsVertical = SizeFlags.FillExpand,

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
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Children =
                {

                    innerTimerPanel
                },
            };

            vSplit.AddChild(TimerFacePlate);
            Contents.AddChild(hSplit);

        }

    }
}
