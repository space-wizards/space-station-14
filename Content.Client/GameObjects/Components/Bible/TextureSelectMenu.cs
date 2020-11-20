using Content.Shared.GameObjects.Components.TextureSelect;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Bible
{
    internal class TextureSelectMenu : SS14Window
    {
        private TextureSelectBoundUserInterface _owner;

        private List<string> _textures;
        private readonly OptionButton _dropDown;

        public TextureSelectMenu(TextureSelectBoundUserInterface owner)
        {
            _owner = owner;
            Title = Loc.GetString("Select Texture"); //TODO: read from yaml?

            _textures = new List<string>();

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

        public void Populate(TextureSelectBoundUserInterfaceState state)
        {
            _textures = state.Textures;
            _dropDown.Clear();

            foreach (var style in _textures)
            {
                _dropDown.AddItem(style);
            }
        }

        private void SelectButton_OnPressed(BaseButton.ButtonEventArgs obj)
        {
            _owner.SelectStyle(_textures[_dropDown.SelectedId]);
        }
    }
}
