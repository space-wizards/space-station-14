using System.Collections.Generic;
using Content.Client.GameObjects.Components.Research;
using Content.Shared.Materials;
using Content.Shared.Research;
using Robust.Client.UserInterface;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private ItemList Items;
        private ItemList Materials;
        private LineEdit AmountLineEdit;
        private LineEdit SearchBar;
        public Button QueueButton;
        public Button ServerConnectButton;
        public Button ServerSyncButton;
        protected override Vector2? CustomSize => (300, 450);

        public LatheBoundUserInterface Owner { get; set; }

        private List<LatheRecipePrototype> _recipes = new List<LatheRecipePrototype>();
        private List<LatheRecipePrototype> _shownRecipes = new List<LatheRecipePrototype>();

        public LatheMenu(LatheBoundUserInterface owner = null)
        {
            IoCManager.InjectDependencies(this);

            Owner = owner;

            Title = "Lathe Menu";

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var vBox = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            var hBoxButtons = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };

            QueueButton = new Button()
            {
                Text = "Queue",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
            };

            ServerConnectButton = new Button()
            {
                Text = "Server list",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
            };

            ServerSyncButton  = new Button()
            {
                Text = "Sync",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
            };

            var spacer = new Control()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3,
            };

            var hBoxFilter = new HBoxContainer()
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

            SearchBar.OnTextChanged += Populate;

            var filterButton = new Button()
            {
                Text = "Filter",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            Items = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SelectMode = ItemList.ItemListSelectMode.Button,
            };

            Items.OnItemSelected += ItemSelected;

            AmountLineEdit = new LineEdit()
            {
                PlaceHolder = "Amount",
                Text = "1",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            AmountLineEdit.OnTextChanged += PopulateDisabled;

            Materials = new ItemList()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3
            };

            hBoxButtons.AddChild(spacer);
            if (Owner?.Database is ProtolatheDatabaseComponent database)
            {
                hBoxButtons.AddChild(ServerConnectButton);
                hBoxButtons.AddChild(ServerSyncButton);
                database.OnDatabaseUpdated += Populate;
            }
            hBoxButtons.AddChild(QueueButton);

            hBoxFilter.AddChild(SearchBar);
            hBoxFilter.AddChild(filterButton);

            vBox.AddChild(hBoxButtons);
            vBox.AddChild(hBoxFilter);
            vBox.AddChild(Items);
            vBox.AddChild(AmountLineEdit);
            vBox.AddChild(Materials);

            margin.AddChild(vBox);

            Contents.AddChild(margin);
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            int.TryParse(AmountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            Owner.Queue(_shownRecipes[args.ItemIndex], quantity);
        }

        public void PopulateMaterials()
        {
            Materials.Clear();

            foreach (var (id, amount) in Owner.Storage)
            {
                if (!_prototypeManager.TryIndex(id, out MaterialPrototype materialPrototype)) continue;
                var material = materialPrototype.Material;
                Materials.AddItem($"{material.Name} {amount} cm³", material.Icon.Frame0(), false);
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
                Items[i].Disabled = !Owner.Lathe.CanProduce(prototype, quantity);
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
            foreach (var prototype in _shownRecipes)
            {
                Items.AddItem(prototype.Name, prototype.Icon.Frame0());
            }

            PopulateDisabled();
        }

        /// <summary>
        ///     Populates the list of recipes that will actually be shown, using the current filters.
        /// </summary>
        public void Populate()
        {
            _shownRecipes.Clear();

            foreach (var prototype in Owner.Database)
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

        /// <inheritdoc cref="Populate"/>
        public void Populate(LineEdit.LineEditEventArgs args)
        {
            Populate();
        }
    }
}
