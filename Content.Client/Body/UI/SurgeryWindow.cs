using System;
using System.Collections.Generic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Body.UI
{
    public class SurgeryWindow : SS14Window
    {
        public delegate void OptionSelectedCallback(int selectedOptionData);

        private readonly BoxContainer _optionsBox;
        private OptionSelectedCallback? _optionSelectedCallback;

        public SurgeryWindow()
        {
            MinSize = SetSize = (300, 400);
            Title = Loc.GetString("surgery-window-title");
            RectClipContent = true;

            var vSplitContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new ScrollContainer
                    {
                        VerticalExpand = true,
                        HorizontalExpand = true,
                        HScrollEnabled = true,
                        VScrollEnabled = true,
                        Children =
                        {
                            (_optionsBox = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                HorizontalExpand = true
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
                _optionSelectedCallback?.Invoke(surgery.CallbackData);
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
                HorizontalExpand = true,
                VerticalExpand = true,
                ToggleMode = true,
                MouseFilter = MouseFilterMode.Stop
            };

            AddChild(Button);

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    (SpriteView = new SpriteView
                    {
                        MinSize = new Vector2(32.0f, 32.0f)
                    }),
                    (DisplayText = new Label
                    {
                        VerticalAlignment = VAlignment.Center,
                        Text = Loc.GetString("surgery-window-not-available-button-text"),
                    }),
                    (new Control
                    {
                        HorizontalExpand = true
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
