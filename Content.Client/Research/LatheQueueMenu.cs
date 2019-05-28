using Content.Client.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.Research
{
    public class LatheQueueMenu : SS14Window
    {
        protected override Vector2? CustomSize => (300, 450);

        public LatheBoundUserInterface Owner { get; set; }

        [ViewVariables]
        private ItemList QueueList;
        private Label NameLabel;
        private Label Description;
        private TextureRect Icon;

        protected override void Initialize()
        {
            base.Initialize();

            HideOnClose = true;
            Title = "Lathe Queue";
            Visible = false;

            var margin = new MarginContainer()
            {
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
            };

            margin.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var vbox = new VBoxContainer();

            vbox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var descMargin = new MarginContainer()
            {
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            var hbox = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            Icon = new TextureRect()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            var vboxInfo = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
            };

            NameLabel = new Label()
            {
                RectClipContent = true,
                SizeFlagsHorizontal = SizeFlags.Fill,
            };

            Description = new Label()
            {
                RectClipContent = true,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.Fill,

            };

            QueueList = new ItemList()
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
                SelectMode = ItemList.ItemListSelectMode.None
            };

            vboxInfo.AddChild(NameLabel);
            vboxInfo.AddChild(Description);

            hbox.AddChild(Icon);
            hbox.AddChild(vboxInfo);

            descMargin.AddChild(hbox);

            vbox.AddChild(descMargin);
            vbox.AddChild(QueueList);

            margin.AddChild(vbox);

            Contents.AddChild(margin);

            ClearInfo();
        }

        public void SetInfo(LatheRecipePrototype recipe)
        {
            Icon.Texture = recipe.Icon.Frame0();
            if (recipe.Name != null)
                NameLabel.Text = recipe.Name;
            if (recipe.Description != null)
                Description.Text = recipe.Description;
        }

        public void ClearInfo()
        {
            Icon.Texture = Texture.Transparent;
            NameLabel.Text = "-------";
            Description.Text = "Not producing anything.";
        }

        public void PopulateList()
        {
            QueueList.Clear();
            var idx = 1;
            foreach (var recipe in Owner.QueuedRecipes)
            {
                QueueList.AddItem($"{idx}. {recipe.Name}", recipe.Icon.Frame0(), false);
                idx++;
            }
        }
    }
}
