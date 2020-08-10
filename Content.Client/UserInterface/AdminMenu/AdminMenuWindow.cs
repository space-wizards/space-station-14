#nullable enable
using Content.Client.UserInterface.AdminMenu;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface
{
    public class AdminMenuWindow : SS14Window
    {
        public TabContainer MasterTabContainer;

        private List<CommandButton> _buttons = new List<CommandButton>
        {
            new KickCommandButton(),
            new TestCommandButton(),
        };
        public AdminMenuWindow()
        {
            Title = Loc.GetString("Admin Menu");

            // Player Tab
            var playerTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var playerButtonGrid = new GridContainer
            {
                Columns = 4,
            };
            foreach (var cmd in _buttons)
            {
                //TODO: make toggle?
                var button = new Button
                {
                    Text = cmd.Name
                };
                button.OnPressed += cmd.ButtonPressed;
                playerButtonGrid.AddChild(button);
            }
            playerTabContainer.AddChild(playerButtonGrid);

            // TODO: Game Tab


            //The master menu that contains all of the tabs.
            MasterTabContainer = new TabContainer();

            //Add all the tabs to the Master container.
            MasterTabContainer.AddChild(playerTabContainer);
            MasterTabContainer.SetTabTitle(0, Loc.GetString("Player"));
            //MasterTabContainer.AddChild(playerTabContainer); //TODO: replace with Game Tab here
            //MasterTabContainer.SetTabTitle(1, Loc.GetString("Game"));
            Contents.AddChild(MasterTabContainer);
        }

        private abstract class CommandButton
        {
            public abstract string Name { get; }
            //abstract _contents
            public string? SubmitText;
            public abstract void Submit(Dictionary<string,string> val);
            public void ButtonPressed(ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new CommandWindow(this);
                window.Submit += Submit;
                manager.OpenCommand(window);
            }
            public abstract List<CommandUIControl> UI { get; }
        }

        private class KickCommandButton : CommandButton
        {
            public override string Name => "Kick";

            public override List<CommandUIControl> UI => new List<CommandUIControl>
            {
                new CommandUIControl
                {
                    Type = CommandUIControlType.DropDown,
                    Name = "Player",
                    Data = new List<string> //TODO: get all players
                    {
                        "Exp",
                        "PJB"
                    }
                },
                new CommandUIControl
                {
                    Type = CommandUIControlType.LineEdit,
                    Name = "Reason",
                    Optional = true
                }
            };

            public override void Submit(Dictionary<string,string> val)
            {
                throw new NotImplementedException();
            }
        }

        private class TestCommandButton : CommandButton
        {
            public override string Name => "Test";

            public override List<CommandUIControl> UI => new List<CommandUIControl>
            {
                new CommandUIControl
                {
                    Type = CommandUIControlType.DropDown,
                    Name = "DropDown",
                    Data = new List<string> //TODO: get all players
                    {
                        "1",
                        "2"
                    }
                },
                new CommandUIControl
                {
                    Type = CommandUIControlType.LineEdit,
                    Name = "LineEdit"
                },
                new CommandUIControl
                {
                    Type = CommandUIControlType.Checkbox,
                    Name = "CheckBox"
                },
                new CommandUIControl
                {
                    Type = CommandUIControlType.LineEdit,
                    Name = "Optional",
                    Optional = true
                },
            };

            public override void Submit(Dictionary<string, string> val)
            {
                throw new NotImplementedException();
            }
        }


        enum CommandUIControlType
        {
            LineEdit,
            DropDown,
            Checkbox
        }
        private class CommandUIControl
        {
            public string Name;
            public CommandUIControlType Type;
            public bool Optional = false;
            public List<string> Data;
            public Control? Control;
        }

        private class CommandWindow : SS14Window
        {
            List<CommandUIControl> _controls;
            public Action<Dictionary<string, string>> Submit { get; set; }
            public CommandWindow(CommandButton button)
            {
                Title = button.Name;
                _controls = button.UI;
                var container = new VBoxContainer //TODO: add margin between different controls
                {
                };
                // Init Controls
                foreach (var control in _controls)
                {
                    var label = new Label
                    {
                        Text = control.Name,
                        CustomMinimumSize = (100, 0)
                    };
                    Control con = control.Type switch
                    {
                        CommandUIControlType.LineEdit => new LineEdit { CustomMinimumSize = (100, 0) },
                        CommandUIControlType.DropDown => new OptionButton { CustomMinimumSize = (100, 0) },
                        CommandUIControlType.Checkbox => new CheckBox { },
                        _ => throw new NotImplementedException(),
                    };
                    var hbox = new HBoxContainer
                    {
                        Children =
                        {
                            label,
                            con
                        },
                    };

                    // Add Items to Dropdown
                    if (con is OptionButton opt)
                    {
                        foreach (var data in control.Data)
                            opt.AddItem(data);

                        opt.OnItemSelected += eventArgs => opt.SelectId(eventArgs.Id);
                    }

                    container.AddChild(hbox);
                    control.Control = con;
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

            string GetValue(CommandUIControl control)
            {
                return control.Control switch
                {
                    LineEdit line => line.Text,
                    CheckBox check => check.Pressed ? "1" : "0",
                    OptionButton opt => control.Data[opt.SelectedId],
                    _ => string.Empty
                };
            }

            public void SubmitPressed(ButtonEventArgs args)
            {
                Dictionary<string, string> val = new Dictionary<string, string>();
                foreach (var control in _controls)
                {
                    if (control.Control == null)
                        return;
                    //TODO: optional check?
                    val.Add(control.Name, GetValue(control));
                }
                Submit.Invoke(val);
            }
        }
    }
}
