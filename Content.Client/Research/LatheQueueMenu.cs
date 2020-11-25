using Content.Client.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.Research
{
    public class LatheQueueMenu : SS14Window
    {
        protected override Vector2? CustomSize => (300, 450);

        public LatheBoundUserInterface Owner { get; set; }

        [ViewVariables]
        private readonly ItemList _queueList;
        private readonly Label _nameLabel;
        private readonly Label _description;
        private readonly TextureRect _icon;

        public LatheQueueMenu()
        {
                        Title = "Lathe Queue";

            var margin = new MarginContainer()
            {
                /*MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,*/
            };

//            margin.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var vBox = new VBoxContainer();

  //          vBox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var descMargin = new MarginContainer()
            {
                /*MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,*/
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            var hBox = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            _icon = new TextureRect()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            var vBoxInfo = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
            };

            _nameLabel = new Label()
            {
                RectClipContent = true,
                SizeFlagsHorizontal = SizeFlags.Fill,
            };

            _description = new Label()
            {
                RectClipContent = true,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.Fill,

            };

            _queueList = new ItemList()
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
                SelectMode = ItemList.ItemListSelectMode.None
            };

            vBoxInfo.AddChild(_nameLabel);
            vBoxInfo.AddChild(_description);

            hBox.AddChild(_icon);
            hBox.AddChild(vBoxInfo);

            descMargin.AddChild(hBox);

            vBox.AddChild(descMargin);
            vBox.AddChild(_queueList);

            margin.AddChild(vBox);

            Contents.AddChild(margin);

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
