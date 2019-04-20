using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Research;
using Content.Shared.Construction;
using Content.Shared.Materials;
using Content.Shared.Research;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timers;
using Robust.Shared.Utility;

namespace Content.Client.Research
{
    public class LatheMenu : SS14Window
    {
#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager PrototypeManager;
        [Dependency]
        private IResourceCache ResourceCache;
#pragma warning restore

        private ItemList Items;
        private ItemList Materials;
        private LineEdit AmountLineEdit;
        private LineEdit SearchBar;
        public Button QueueButton;
        protected override Vector2? CustomSize => (300, 450);

        public LatheComponent Owner { get; set; }

        private List<LatheRecipePrototype> _recipes = new List<LatheRecipePrototype>();
        private List<LatheRecipePrototype> _shownRecipes = new List<LatheRecipePrototype>();

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

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
            };

            margin.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var vbox = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            vbox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var hboxButtons = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };

            QueueButton = new Button()
            {
                Text = "Queue",
                TextAlign = Button.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };

            var spacer = new Control()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            spacer.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            var materialButton = new Button()
            {
                Text = "Materials",
                TextAlign = Button.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            var hboxFilter = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };

            SearchBar = new LineEdit()
            {
                PlaceHolder = "Search Designs",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3
            };

            SearchBar.OnTextChanged += PopulateFilter;

            var filterButton = new Button()
            {
                Text = "Filter",
                TextAlign = Button.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            var scrollContainer = new ScrollContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 8
            };

            Items = new ItemList()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
            };



            Items.OnItemSelected += ItemSelected;

            AmountLineEdit = new LineEdit()
            {
                PlaceHolder = "Amount",
                Text = "1",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            AmountLineEdit.OnTextChanged += PopulateDisabled;

            var scrollMaterialContainer = new ScrollContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3
            };

            Materials = new ItemList()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
            };

            hboxButtons.AddChild(QueueButton);
            hboxButtons.AddChild(spacer);
            hboxButtons.AddChild(materialButton);

            hboxFilter.AddChild(SearchBar);
            hboxFilter.AddChild(filterButton);

            scrollContainer.AddChild(Items);
            scrollMaterialContainer.AddChild(Materials);

            vbox.AddChild(hboxButtons);
            vbox.AddChild(hboxFilter);
            vbox.AddChild(scrollContainer);
            vbox.AddChild(AmountLineEdit);
            vbox.AddChild(scrollMaterialContainer);

            margin.AddChild(vbox);

            Contents.AddChild(margin);
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            int.TryParse(AmountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            Owner.Queue(_shownRecipes[args.ItemIndex], quantity);
            Items.SelectMode = ItemList.ItemListSelectMode.None;
            Timer.Spawn(100, () => {Items.Unselect(args.ItemIndex);});
            Timer.Spawn(100, () => {Items.SelectMode = ItemList.ItemListSelectMode.Single;});
        }

        public void PopulateMaterials()
        {
            Materials.Clear();
            Owner.Owner.TryGetComponent(out MaterialStorageComponent storage);

            if (storage == null) return;

            foreach (var (id, amount) in storage)
            {
                Material.TryGetMaterial(id, out var material);
                Materials.AddItem($"{material.Name} {amount} cm3", material.Icon.Frame0(), false);
            }
        }

        /// <summary>
        ///     Disables or enables shown recipes depending on whether there are enough materials for it or not.
        /// </summary>
        public void PopulateDisabled()
        {
            int.TryParse(AmountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            for (var i = 0; i < _shownRecipes.Count; i++)
            {
                var prototype = _shownRecipes[i];
                Items.SetItemDisabled(i, !Owner.CanProduce(prototype, quantity));
            }
        }

        /// <inheritdoc cref="PopulateDisabled()"/>
        public void PopulateDisabled(LineEdit.LineEditEventArgs args)
        {
            PopulateDisabled();
        }

        /// <summary>
        ///     Adds shown recipes to the ItemList control.
        /// </summary>
        public void PopulateList()
        {
            Items.Clear();
            for (var i = 0; i < _shownRecipes.Count; i++)
            {
                var prototype = _shownRecipes[i];
                Items.AddItem(prototype.Name, prototype.Icon.Frame0());
            }

            PopulateDisabled();
        }

        /// <summary>
        ///     Populates the list of recipes that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateFilter()
        {
            _shownRecipes.Clear();

            foreach (var prototype in _recipes)
            {
                if (SearchBar.Text.Trim().Length != 0)
                {
                    if (prototype.Name.ToLowerInvariant().Contains(SearchBar.Text.Trim().ToLowerInvariant()))
                        _shownRecipes.Add(prototype);
                    continue;
                }

                _shownRecipes.Add(prototype);
            }

            PopulateList();
        }

        /// <inheritdoc cref="PopulateFilter()"/>
        public void PopulateFilter(LineEdit.LineEditEventArgs args)
        {
            PopulateFilter();
        }

        /// <summary>
        ///     Populates the recipe list with recipes this lathe has unlocked
        /// </summary>
        public void PopulateRecipes()
        {
            _recipes.Clear();

            if (PrototypeManager == null)
                PrototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var prototype in PrototypeManager.EnumeratePrototypes<LatheRecipePrototype>())
            {
                // TODO: Check if the prototype is unlocked...
                if (prototype.LatheType == Owner.LatheType)
                    _recipes.Add(prototype);
            }
            PopulateFilter();
        }
    }
}
