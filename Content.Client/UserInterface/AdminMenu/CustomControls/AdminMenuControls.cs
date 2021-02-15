#nullable enable
using System;
using System.Collections.Generic;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.AdminMenu.CustomControls
{
    public class AdminMenuControls
    {
        #region CommandButtonBaseClass

        public abstract class CommandButton
        {
            public virtual string Name { get; }
            public virtual string RequiredCommand { get; }
            public abstract void ButtonPressed(BaseButton.ButtonEventArgs args);

            public virtual bool CanPress()
            {
                return RequiredCommand == string.Empty ||
                       IoCManager.Resolve<IClientConGroupController>().CanCommand(RequiredCommand);
            }

            public CommandButton() : this(string.Empty, string.Empty) { }

            public CommandButton(string name, string command)
            {
                Name = name;
                RequiredCommand = command;
            }
        }

        // Button that opens a UI
        public abstract class UICommandButton : CommandButton
        {
            // The text on the submit button
            public virtual string? SubmitText { get; }

            /// <summary>
            /// Called when the Submit button is pressed
            /// </summary>
            /// <param name="val">Dictionary of the parameter names and values</param>
            public abstract void Submit();

            public override void ButtonPressed(BaseButton.ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new CommandWindow(this);
                window.Submit += Submit;
                manager.OpenCommand(window);
            }

            // List of all the UI Elements
            public abstract List<CommandUIControl> UI { get; }
        }

        // Button that directly calls a Command
        public class DirectCommandButton : CommandButton
        {
            public DirectCommandButton(string name, string command) : base(name, command) { }

            public override void ButtonPressed(BaseButton.ButtonEventArgs args)
            {
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(RequiredCommand);
            }
        }

        #endregion

        #region CommandUIControls

        public abstract class CommandUIControl
        {
            public string? Name;
            public Control? Control;
            public abstract Control GetControl();
            public abstract string GetValue();
        }

        public class CommandUIDropDown : CommandUIControl
        {
            public Func<List<object>>? GetData;

            // The string that the player sees in the list
            public Func<object, string>? GetDisplayName;

            // The value that is given to Submit
            public Func<object, string>? GetValueFromData;

            // Cache
            protected List<object>?
                Data; //TODO: make this like IEnumerable or smth, so you don't have to do this ToList<object> shittery

            public override Control GetControl() //TODO: fix optionbutton being shitty after moving the window
            {
                var opt = new OptionButton
                    {CustomMinimumSize = (100, 0), SizeFlagsHorizontal = Control.SizeFlags.FillExpand};
                Data = GetData!();
                foreach (var item in Data)
                    opt.AddItem(GetDisplayName!(item));

                opt.OnItemSelected += eventArgs => opt.SelectId(eventArgs.Id);
                Control = opt;
                return Control;
            }

            public override string GetValue()
            {
                return GetValueFromData!(Data![((OptionButton) Control!).SelectedId]);
            }
        }

        public class CommandUICheckBox : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new CheckBox
                {
                    SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
                };
                return Control;
            }

            public override string GetValue()
            {
                return ((CheckBox) Control!).Pressed ? "1" : "0";
            }
        }

        public class CommandUILineEdit : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new LineEdit
                    {CustomMinimumSize = (100, 0), SizeFlagsHorizontal = Control.SizeFlags.FillExpand};
                return Control;
            }

            public override string GetValue()
            {
                return ((LineEdit) Control!).Text;
            }
        }

        public class CommandUISpinBox : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new SpinBox
                    {CustomMinimumSize = (100, 0), SizeFlagsHorizontal = Control.SizeFlags.FillExpand};
                return Control;
            }

            public override string GetValue()
            {
                return ((SpinBox) Control!).Value.ToString();
            }
        }

        public class CommandUIButton : CommandUIControl
        {
            public Action? Handler { get; set; }

            public override Control GetControl()
            {
                Control = new Button
                {
                    CustomMinimumSize = (100, 0),
                    SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                    Text = Name
                };
                return Control;
            }

            public override string GetValue()
            {
                return "";
            }
        }

        #endregion

        #region CommandWindow

        private class CommandWindow : SS14Window
        {
            List<CommandUIControl> _controls;
            public Action? Submit { get; set; }

            public CommandWindow(UICommandButton button)
            {
                Title = button.Name;
                _controls = button.UI;
                var container = new VBoxContainer //TODO: add margin between different controls
                {
                };
                // Init Controls in a hbox + a label
                foreach (var control in _controls)
                {
                    var c = control.GetControl();
                    if (c is Button)
                    {
                        ((Button) c).OnPressed += (args) =>
                        {
                            ((CommandUIButton) control).Handler?.Invoke();
                        };
                        container.AddChild(c);
                    }
                    else
                    {
                        var label = new Label
                        {
                            Text = control.Name,
                            CustomMinimumSize = (100, 0)
                        };
                        var divider = new Control
                        {
                            CustomMinimumSize = (50, 0)
                        };
                        var hbox = new HBoxContainer
                        {
                            Children =
                            {
                                label,
                                divider,
                                c
                            },
                        };
                        container.AddChild(hbox);
                    }
                }

                // Init Submit Button
                var submitButton = new Button
                {
                    Text = button.SubmitText ?? button.Name
                };
                submitButton.OnPressed += SubmitPressed;
                container.AddChild(submitButton);

                Contents.AddChild(container);
            }

            public void SubmitPressed(BaseButton.ButtonEventArgs args)
            {
                Submit?.Invoke();
            }
        }

        #endregion
    }
}
