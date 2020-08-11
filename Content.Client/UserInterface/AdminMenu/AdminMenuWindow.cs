#nullable enable
using Content.Client.UserInterface.AdminMenu;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
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
            //TODO: we check here the commands, but what if we join a new server? the window gets created at game launch
            // create the window everytime we open it?
            var groupController = IoCManager.Resolve<IClientConGroupController>(); 
            foreach (var cmd in _buttons)
            {
                // Check if the player can do the command
                if (cmd.RequiredCommand != string.Empty && !groupController.CanCommand(cmd.RequiredCommand))
                    continue;

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
            public abstract string RequiredCommand { get; }
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
            public override string RequiredCommand => "kick";

            public override List<CommandUIControl> UI => new List<CommandUIControl>
            {
                new CommandUIDropDown
                {
                    Name = "Player",
                    GetData = () => IoCManager.Resolve<IPlayerManager>().Sessions.ToList<object>(),
                    GetDisplayName = (obj) => $"{((IPlayerSession)obj).Name} ({((IPlayerSession)obj).AttachedEntity?.Name})",
                    GetValueFromData = (obj) => ((IPlayerSession)obj).Name,
                },
                new CommandUILineEdit
                {
                    Name = "Reason",
                    Optional = true
                }
            };

            public override void Submit(Dictionary<string,string> val)
            {
                IoCManager.Resolve<IClientConsole>().ProcessCommand($"kick \"{val["Player"]}\" \"{val["Reason"]}\"");
            }
        }

        private class TestCommandButton : CommandButton
        {
            public override string Name => "Test";

            public override string RequiredCommand => string.Empty;

            public override List<CommandUIControl> UI => new List<CommandUIControl>
            {
                new CommandUIDropDown
                {
                    Name = "DropDown",
                    GetData = () => new List<object>
                    {
                        "1",
                        "2"
                    },
                    GetValueFromData = (obj) => (string)obj
                },
                new CommandUILineEdit
                {
                    Name = "LineEdit"
                },
                new CommandUICheckBox
                {
                    Name = "CheckBox"
                },
                new CommandUILineEdit
                {
                    Name = "Optional",
                    Optional = true
                },
            };

            public override void Submit(Dictionary<string, string> val)
            {
                IoCManager.Resolve<IClientConsole>().ProcessCommand($"say \"Dropdown: {val["DropDown"]}\nLineEdit: {val["LineEdit"]}\nCheckBox: {val["CheckBox"]}\nOptional: {val["Optional"]}\"");
            }
        }

        //do we really need this? can't we just give the control to the poor window?
        private abstract class CommandUIControl
        {
            public string Name;
            public bool Optional = false;
            public Control Control;
            //Idea: implement these abstract functions:
            public abstract Control GetControl();
            public abstract string GetValue();
        }
        private class CommandUIDropDown : CommandUIControl
        {
            public Func<List<object>> GetData;
            // The string that the player sees in the list
            public Func<object, string> GetDisplayName;
            // The value that is given to Submit
            public Func<object, string> GetValueFromData;
            // Cache
            public List<object> Data;

            public override Control GetControl()
            {
                var opt = new OptionButton { CustomMinimumSize = (100, 0) };
                Data = GetData();
                foreach (var item in Data)
                    opt.AddItem(GetDisplayName(item));

                opt.OnItemSelected += eventArgs => opt.SelectId(eventArgs.Id);
                Control = opt;
                return Control;
            }

            public override string GetValue()
            {
                return GetValueFromData(Data[((OptionButton)Control).SelectedId]);
            }
        }
        private class CommandUICheckBox : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new CheckBox();
                return Control;
            }

            public override string GetValue()
            {
                return ((CheckBox)Control).Pressed ? "1" : "0";
            }
        }
        private class CommandUILineEdit : CommandUIControl
        {
            public override Control GetControl()
            {
                Control = new LineEdit { CustomMinimumSize = (100, 0) };
                return Control;
            }

            public override string GetValue()
            {
                return ((LineEdit)Control).Text;
            }
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
                // Init Controls in a hbox + a label
                foreach (var control in _controls)
                {
                    var label = new Label
                    {
                        Text = control.Name,
                        CustomMinimumSize = (100, 0)
                    };
                    var divider = new Control
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand
                    };
                    var hbox = new HBoxContainer
                    {
                        Children =
                        {
                            label,
                            divider,
                            control.GetControl()
                        },
                    };

                    container.AddChild(hbox);
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

            public void SubmitPressed(ButtonEventArgs args)
            {
                Dictionary<string, string> val = new Dictionary<string, string>();
                foreach (var control in _controls)
                {
                    if (control.Control == null)
                        return;
                    //TODO EXP: optional check?
                    val.Add(control.Name, control.GetValue());
                }
                Submit.Invoke(val);
            }
        }
    }
}
