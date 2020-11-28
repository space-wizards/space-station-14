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

        private readonly ItemList _items;
        private readonly ItemList _materials;
        private readonly LineEdit _amountLineEdit;
        private readonly LineEdit _searchBar;
        public Button QueueButton;
        public Button ServerConnectButton;
        public Button ServerSyncButton;
        protected override Vector2? CustomSize => (300, 450);

        public LatheBoundUserInterface Owner { get; set; }

        private readonly List<LatheRecipePrototype> _shownRecipes = new();

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

            _searchBar = new LineEdit()
            {
                PlaceHolder = "Search Designs",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3
            };

            _searchBar.OnTextChanged += Populate;

            var filterButton = new Button()
            {
                Text = "Filter",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            _items = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SelectMode = ItemList.ItemListSelectMode.Button,
            };

            _items.OnItemSelected += ItemSelected;

            _amountLineEdit = new LineEdit()
            {
                PlaceHolder = "Amount",
                Text = "1",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            _amountLineEdit.OnTextChanged += PopulateDisabled;

            _materials = new ItemList()
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

            hBoxFilter.AddChild(_searchBar);
            hBoxFilter.AddChild(filterButton);

            vBox.AddChild(hBoxButtons);
            vBox.AddChild(hBoxFilter);
            vBox.AddChild(_items);
            vBox.AddChild(_amountLineEdit);
            vBox.AddChild(_materials);

            margin.AddChild(vBox);

            Contents.AddChild(margin);
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            int.TryParse(_amountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            Owner.Queue(_shownRecipes[args.ItemIndex], quantity);
        }

        public void PopulateMaterials()
        {
            _materials.Clear();

            foreach (var (id, amount) in Owner.Storage)
            {
                if (!_prototypeManager.TryIndex(id, out MaterialPrototype materialPrototype)) continue;
                var material = materialPrototype.Material;
                _materials.AddItem($"{material.Name} {amount} cm³", material.Icon.Frame0(), false);
            }
        }

        /// <summary>
        ///     Disables or enables shown recipes depending on whether there are enough materials for it or not.
        /// </summary>
        public void PopulateDisabled()
        {
            int.TryParse(_amountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            for (var i = 0; i < _shownRecipes.Count; i++)
            {
                var prototype = _shownRecipes[i];
                _items[i].Disabled = !Owner.Lathe.CanProduce(prototype, quantity);
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
            _items.Clear();
            foreach (var prototype in _shownRecipes)
            {
                _items.AddItem(prototype.Name, prototype.Icon.Frame0());
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
                if (_searchBar.Text.Trim().Length != 0)
                {
                    if (prototype.Name.ToLowerInvariant().Contains(_searchBar.Text.Trim().ToLowerInvariant()))
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
