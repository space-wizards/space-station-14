using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Research
{
    public class LatheMenu : SS14Window
    {
#pragma warning disable CS0649
        [Dependency]
        private readonly IPrototypeManager PrototypeManager;
        [Dependency]
        private readonly IResourceCache ResourceCache;
#pragma warning restore

        private ItemList Items;
        private Label RecipeName;
        private Label RecipeDescription;
        private TextureRect RecipeIcon;
        private Button ProduceButton;
        protected override Vector2? CustomSize => (758, 431);

        public LatheComponent Owner { get; set; }
        public readonly List<LatheRecipePrototype> Recipes = new List<LatheRecipePrototype>();

        public LatheMenu(IDisplayManager displayMan) : base(displayMan)
        {
        }

        public LatheMenu(IDisplayManager displayMan, string name) : base(displayMan, name)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            HideOnClose = true;
            Title = "Lathe Menu";
            Visible = false;

            var hbox = new HBoxContainer();

            Contents.AddChild(hbox);

            hbox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var scroll = new ScrollContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };

            hbox.AddChild(scroll);

            Items = new ItemList()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            Items.SetAnchorAndMarginPreset(LayoutPreset.Wide);
            Items.OnItemSelected += ItemSelected;
            Items.OnItemDeselected += ItemDeselected;

            scroll.AddChild(Items);

            var vbox = new VBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            RecipeName = new Label()
            {
                Align = Label.AlignMode.Center,
            };

            RecipeIcon = new TextureRect();

            vbox.AddChild(RecipeName);

            RecipeDescription = new Label();

            vbox.AddChild(RecipeDescription);

            hbox.AddChild(vbox);

            AddToScreen();
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            var recipe = Recipes[args.ItemIndex];
            RecipeName.Text = recipe.Name;
            RecipeDescription.Text = recipe.Description;
            RecipeIcon.Texture = recipe.Icon.Frame0();
            ProduceButton.Disabled = false;
        }



        public void ItemDeselected(ItemList.ItemListDeselectedEventArgs args)
        {
            RecipeName.Text = "";
            RecipeDescription.Text = "";
            RecipeIcon.Texture = null;
            ProduceButton.Disabled = true;
        }

        public void Populate()
        {
            Recipes.Clear();

            foreach (var prototype in PrototypeManager.EnumeratePrototypes<LatheRecipePrototype>())
            {
                // Here it should check if the prototype is unlocked...

            }
        }

        public class RecipeButton
        {
            public LatheRecipePrototype Recipe;

            public LatheMenu Menu
            {
                get;
                set;
            }


        }
    }
}
