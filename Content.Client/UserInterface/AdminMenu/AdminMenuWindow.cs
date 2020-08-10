using NFluidsynth;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface
{
    public class AdminMenuWindow : SS14Window
    {
        public TabContainer MasterTabContainer;

        //InitLayout
        /*
         *  Tabs:
         *  Game [Player] Server 
         *  Buttons:
         *  GridContainer:
         *  Kick Ban MakeAdmin
         *  
         */

        private List<CommandButton> _buttons = new List<CommandButton>{
                new CommandButton
                {
                    Name = "Kick"
                },
                new CommandButton
                {
                    Name = "Ban"
                },
                new CommandButton
                {
                    Name = "Make Admin"
                },
                new CommandButton
                {
                    Name = "Message"
                },
                new CommandButton
                {
                    Name = "Button"
                }
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

        private class CommandButton
        {
            public string Name;
            //abstract _contents
            //abstract Submit();
            public void ButtonPressed(ButtonEventArgs args)
            {

            }
        }
    }
}
