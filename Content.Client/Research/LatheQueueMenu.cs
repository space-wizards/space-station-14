using Content.Client.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Client.Research
{
    public class LatheQueueMenu : SS14Window
    {
        public LatheBoundUserInterface Owner { get; set; }

        [ViewVariables]
        private readonly ItemList _queueList;
        private readonly Label _nameLabel;
        private readonly Label _description;
        private readonly TextureRect _icon;

        public LatheQueueMenu(LatheBoundUserInterface owner)
        {
            Owner = owner;
            SetSize = MinSize = (300, 450);
            Title = Loc.GetString("lathe-queue");

            var vBox = new VBoxContainer();

            var hBox = new HBoxContainer()
            {
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 2,
            };

            _icon = new TextureRect()
            {
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 2,
            };

            var vBoxInfo = new VBoxContainer()
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 3,
            };

            _nameLabel = new Label()
            {
                RectClipContent = true,
            };

            _description = new Label()
            {
                RectClipContent = true,
                VerticalAlignment = VAlignment.Stretch,
                VerticalExpand = true

            };

            _queueList = new ItemList()
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 3,
                SelectMode = ItemList.ItemListSelectMode.None
            };

            vBoxInfo.AddChild(_nameLabel);
            vBoxInfo.AddChild(_description);

            hBox.AddChild(_icon);
            hBox.AddChild(vBoxInfo);

            vBox.AddChild(hBox);
            vBox.AddChild(_queueList);

            Contents.AddChild(vBox);

            ClearInfo();
        }

        public void SetInfo(LatheRecipePrototype recipe)
        {
            _icon.Texture = recipe.Icon.Frame0();
            if (recipe.Name != null)
                _nameLabel.Text = recipe.Name;
            if (recipe.Description != null)
                _description.Text = recipe.Description;
        }

        public void ClearInfo()
        {
            _icon.Texture = Texture.Transparent;
            _nameLabel.Text = "-------";
            _description.Text = "Not producing anything.";
        }

        public void PopulateList()
        {
            _queueList.Clear();
            var idx = 1;
            foreach (var recipe in Owner.QueuedRecipes)
            {
                _queueList.AddItem($"{idx}. {recipe.Name}", recipe.Icon.Frame0());
                idx++;
            }
        }
    }
}
