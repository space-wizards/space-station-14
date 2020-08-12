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
        public VBoxContainer PlayerList;

        private List<CommandButton> _buttons = new List<CommandButton>
        {
            new KickCommandButton(),
            new TestCommandButton(),
            new DirectCommandButton("Restart Round", "restartround"),
        };

        private void RefreshPlayerList(ButtonEventArgs args)
        {
            PlayerList.RemoveAllChildren();
            var sessions = IoCManager.Resolve<IPlayerManager>().Sessions;
            var header = new HBoxContainer
            {
                Children =
                    {
                        new Label { Text = "Name" },
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = "Player"},
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = "Status"},
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = "Ping"},
                    }
            };
            PlayerList.AddChild(header);
            PlayerList.AddChild(new Controls.HighDivider());
            foreach (var player in sessions) //TODO: make this aligned with the header
            {
                var hbox = new HBoxContainer
                {
                    Children =
                    {
                        new Label { Text = player.Name },
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = player.AttachedEntity?.Name },
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = player.Status.ToString() },
                        new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                        new Label { Text = player.Ping.ToString() },
                    }
                };
                PlayerList.AddChild(hbox);
            }
        }
        public AdminMenuWindow() //TODO: search for buttons?
        {
            CustomMinimumSize = (415,0);
            Title = Loc.GetString("Admin Menu");

            // Players
            var playerTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            PlayerList = new VBoxContainer();
            var refreshButton = new Button
            {
                Text = "Refresh"
            };
            refreshButton.OnPressed += RefreshPlayerList;
            RefreshPlayerList(null);
            var playerVBox = new VBoxContainer
            {
                Children =
                {
                    refreshButton,
                    PlayerList
                }
            };
            playerTabContainer.AddChild(playerVBox);

            // Admin Tab
            var adminTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };
            var adminButtonGrid = new GridContainer
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
                adminButtonGrid.AddChild(button);
            }
            adminTabContainer.AddChild(adminButtonGrid);

            // Adminbus
            var adminbusTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };

            // Debug
            var debugTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };

            // Round
            var roundTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };

            // Server
            var serverTabContainer = new MarginContainer
            {
                MarginLeftOverride = 4,
                MarginTopOverride = 4,
                MarginRightOverride = 4,
                MarginBottomOverride = 4,
                CustomMinimumSize = (50, 50),
            };


            //The master menu that contains all of the tabs.
            MasterTabContainer = new TabContainer();

            //Add all the tabs to the Master container.
            //TODO: add playerlist maybe?
            MasterTabContainer.AddChild(adminTabContainer);
            MasterTabContainer.SetTabTitle(0, Loc.GetString("Admin"));
            MasterTabContainer.AddChild(adminbusTabContainer);
            MasterTabContainer.SetTabTitle(1, Loc.GetString("Adminbus"));
            MasterTabContainer.AddChild(debugTabContainer);
            MasterTabContainer.SetTabTitle(2, Loc.GetString("Debug"));
            MasterTabContainer.AddChild(roundTabContainer);
            MasterTabContainer.SetTabTitle(3, Loc.GetString("Round"));
            MasterTabContainer.AddChild(serverTabContainer);
            MasterTabContainer.SetTabTitle(4, Loc.GetString("Server"));
            MasterTabContainer.AddChild(playerTabContainer);
            MasterTabContainer.SetTabTitle(5, Loc.GetString("Players"));
            Contents.AddChild(MasterTabContainer);
        }

        private abstract class CommandButton
        {
            public virtual string Name { get; }
            public virtual string RequiredCommand { get; }
            public abstract void ButtonPressed(ButtonEventArgs args);

            public CommandButton()
            {
                Name = string.Empty;
                RequiredCommand = string.Empty;
            }
            public CommandButton(string name, string command)
            {
                Name = name;
                RequiredCommand = command;
            }
        }

        // Button that opens a UI
        private abstract class UICommandButton : CommandButton
        {
            public string? SubmitText;
            public abstract void Submit(Dictionary<string, string> val);
            public override void ButtonPressed(ButtonEventArgs args)
            {
                var manager = IoCManager.Resolve<IAdminMenuManager>();
                var window = new CommandWindow(this);
                window.Submit += Submit;
                manager.OpenCommand(window);
            }
            public abstract List<CommandUIControl> UI { get; }
        }

        // Button that directly calls a Command
        private class DirectCommandButton : CommandButton
        {
            public DirectCommandButton(string name, string command) : base(name, command) { }

            public override void ButtonPressed(ButtonEventArgs args)
            {
                IoCManager.Resolve<IClientConsole>().ProcessCommand(RequiredCommand);
            }
        }

        private class KickCommandButton : UICommandButton
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

        private class TestCommandButton : UICommandButton
        {
            public override string Name => "Test";

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
            private List<object> Data;

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
