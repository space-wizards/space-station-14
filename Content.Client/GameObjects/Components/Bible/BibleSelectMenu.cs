using Content.Shared.GameObjects.Components.Bible;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.GameObjects.Components.Bible
{
    internal class BibleSelectMenu : SS14Window
    {
        private BibleBoundUserInterface _owner;

        private List<string> _styles;
        private OptionButton _dropDown;

        public BibleSelectMenu(BibleBoundUserInterface owner)
        {
            _owner = owner;
            Title = Loc.GetString("Select Bible Style");

            _styles = new List<string>();

            var hBox = new HBoxContainer();
            Contents.AddChild(hBox);

            _dropDown = new OptionButton();
            _dropDown.OnItemSelected += eventArgs => _dropDown.SelectId(eventArgs.Id);

            hBox.AddChild(_dropDown);

            var selectButton = new Button()
            {
                Text = Loc.GetString("Select")
            };
            selectButton.OnPressed += SelectButton_OnPressed;
            hBox.AddChild(selectButton);
        }

        public void Populate(BibleBoundUserInterfaceState state)
        {
            _styles = state.Styles;
            _dropDown.Clear();

            foreach (var style in _styles)
            {
                _dropDown.AddItem(style);
            }
        }

        private void SelectButton_OnPressed(BaseButton.ButtonEventArgs obj)
        {
            _owner.SelectStyle(_styles[_dropDown.SelectedId]);
        }
    }
}
