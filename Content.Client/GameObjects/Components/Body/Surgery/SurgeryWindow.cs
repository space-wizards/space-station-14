using System;
using System.Collections.Generic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Body.Surgery
{
    public class SurgeryWindow : SS14Window
    {
        public delegate void OptionSelectedCallback(int selectedOptionData);

        private readonly VBoxContainer _optionsBox;
        private OptionSelectedCallback _optionSelectedCallback;

        protected override Vector2? CustomSize => (300, 400);

        public SurgeryWindow()
        {
            Title = Loc.GetString("Surgery");
            RectClipContent = true;

            var vSplitContainer = new VBoxContainer
            {
                Children =
                {
                    new ScrollContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        HScrollEnabled = true,
                        VScrollEnabled = true,
                        Children =
                        {
                            (_optionsBox = new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand
                            })
                        }
                    }
                }
            };

            Contents.AddChild(vSplitContainer);
        }

        public void BuildDisplay(Dictionary<string, int> data, OptionSelectedCallback callback)
        {
            _optionsBox.DisposeAllChildren();
            _optionSelectedCallback = callback;

            foreach (var (displayText, callbackData) in data)
            {
                var button = new SurgeryButton(callbackData);

                button.SetOnToggleBehavior(OnButtonPressed);
                button.SetDisplayText(Loc.GetString(displayText));

                _optionsBox.AddChild(button);
            }
        }

        private void OnButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Button.Parent is SurgeryButton surgery)
            {
                _optionSelectedCallback(surgery.CallbackData);
            }
        }
    }

    class SurgeryButton : PanelContainer
    {
        public Button Button { get; }

        private SpriteView SpriteView { get; }

        private Label DisplayText { get; }

        public int CallbackData { get; }

        public SurgeryButton(int callbackData)
        {
            CallbackData = callbackData;

            Button = new Button
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                ToggleMode = true,
                MouseFilter = MouseFilterMode.Stop
            };

            AddChild(Button);

            AddChild(new HBoxContainer
            {
                Children =
                {
                    (SpriteView = new SpriteView
                    {
                        CustomMinimumSize = new Vector2(32.0f, 32.0f)
                    }),
                    (DisplayText = new Label
                    {
                        SizeFlagsVertical = SizeFlags.ShrinkCenter,
                        Text = "N/A",
                    }),
                    (new Control
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand
                    })
                }
            });
        }

        public void SetDisplayText(string text)
        {
            DisplayText.Text = text;
        }

        public void SetOnToggleBehavior(Action<BaseButton.ButtonToggledEventArgs> behavior)
        {
            Button.OnToggled += behavior;
        }

        public void SetSprite()
        {
            //button.SpriteView.Sprite = sprite;
        }
    }
}
