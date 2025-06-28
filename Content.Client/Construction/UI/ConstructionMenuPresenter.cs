using System.Linq;
using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI
{
    /// <summary>
    /// This class presents the Construction/Crafting UI to the client, linking the <see cref="ConstructionSystem" /> with the
    /// model. This is where the bulk of UI work is done, either calling functions in the model to change state, or collecting
    /// data out of the model to *present* to the screen though the UI framework.
    /// </summary>
    internal sealed class ConstructionMenuPresenter : IDisposable
    {
        [Dependency] private readonly EntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        private readonly SpriteSystem _spriteSystem;

        private readonly IConstructionMenuView _constructionView;
        private readonly EntityWhitelistSystem _whitelistSystem;

        private ConstructionSystem? _constructionSystem;
        private ConstructionPrototype? _selected;
        private List<ConstructionPrototype> _favoritedRecipes = [];
        private readonly Dictionary<string, ContainerButton> _recipeButtons = new();
        private string _selectedCategory = string.Empty;

        private const string FavoriteCatName = "construction-category-favorites";
        private const string ForAllCategoryName = "construction-category-all";

        private bool CraftingAvailable
        {
            get => _uiManager.GetActiveUIWidget<GameTopMenuBar>().CraftingButton.Visible;
            set
            {
                _uiManager.GetActiveUIWidget<GameTopMenuBar>().CraftingButton.Visible = value;
                if (!value)
                    _constructionView.Close();
            }
        }

        /// <summary>
        /// Does the window have focus? If the window is closed, this will always return false.
        /// </summary>
        private bool IsAtFront => _constructionView.IsOpen && _constructionView.IsAtFront();

        private bool WindowOpen
        {
            get => _constructionView.IsOpen;
            set
            {
                if (value && CraftingAvailable)
                {
                    if (_constructionView.IsOpen)
                        _constructionView.MoveToFront();
                    else
                        _constructionView.OpenCentered();

                    if (_selected != null)
                        PopulateInfo(_selected);
                }
                else
                    _constructionView.Close();
            }
        }

        /// <summary>
        /// Constructs a new instance of <see cref="ConstructionMenuPresenter" />.
        /// </summary>
        public ConstructionMenuPresenter()
        {
            // This is a lot easier than a factory
            IoCManager.InjectDependencies(this);
            _constructionView = new ConstructionMenu();
            _whitelistSystem = _entManager.System<EntityWhitelistSystem>();
            _spriteSystem = _entManager.System<SpriteSystem>();

            // This is required so that if we load after the system is initialized, we can bind to it immediately
            if (_systemManager.TryGetEntitySystem<ConstructionSystem>(out var constructionSystem))
                SystemBindingChanged(constructionSystem);

            _systemManager.SystemLoaded += OnSystemLoaded;
            _systemManager.SystemUnloaded += OnSystemUnloaded;

            _placementManager.PlacementChanged += OnPlacementChanged;

            _constructionView.OnClose +=
                () => _uiManager.GetActiveUIWidget<GameTopMenuBar>().CraftingButton.Pressed = false;
            _constructionView.ClearAllGhosts += (_, _) => _constructionSystem?.ClearAllGhosts();
            _constructionView.PopulateRecipes += OnViewPopulateRecipes;
            _constructionView.RecipeSelected += OnViewRecipeSelected;
            _constructionView.BuildButtonToggled += (_, b) => BuildButtonToggled(b);
            _constructionView.EraseButtonToggled += (_, b) =>
            {
                if (_constructionSystem is null)
                    return;
                if (b)
                    _placementManager.Clear();
                _placementManager.ToggleEraserHijacked(new ConstructionPlacementHijack(_constructionSystem, null));
                _constructionView.EraseButtonPressed = b;
            };

            _constructionView.RecipeFavorited += (_, _) => OnViewFavoriteRecipe();

            SetFavorites(_preferencesManager.Preferences?.ConstructionFavorites ?? []);
            OnViewPopulateRecipes(_constructionView, (string.Empty, string.Empty));
        }

        public void OnHudCraftingButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            WindowOpen = args.Pressed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _constructionView.Dispose();

            SystemBindingChanged(null);
            _systemManager.SystemLoaded -= OnSystemLoaded;
            _systemManager.SystemUnloaded -= OnSystemUnloaded;

            _placementManager.PlacementChanged -= OnPlacementChanged;
        }

        private void OnPlacementChanged(object? sender, EventArgs e)
        {
            _constructionView.ResetPlacement();
        }

        private void OnViewRecipeSelected(object? sender, ConstructionMenu.ConstructionMenuListData? item)
        {
            if (item is null)
            {
                _selected = null;
                _constructionView.ClearRecipeInfo();
                return;
            }

            _selected = item.Prototype;

            if (_placementManager is { IsActive: true, Eraser: false })
                UpdateGhostPlacement();

            PopulateInfo(_selected);
        }

        private void OnGridViewRecipeSelected(object? _, ConstructionPrototype? recipe)
        {
            if (recipe is null)
            {
                _selected = null;
                _constructionView.ClearRecipeInfo();
                return;
            }

            _selected = recipe;

            if (_placementManager is { IsActive: true, Eraser: false })
                UpdateGhostPlacement();

            PopulateInfo(_selected);
        }

        private void OnViewPopulateRecipes(object? sender, (string search, string catagory) args)
        {
            if (_constructionSystem is null)
                return;

            var actualRecipes = GetAndSortRecipes(args);

            var recipesList = _constructionView.Recipes;
            var recipesGrid = _constructionView.RecipesGrid;
            recipesGrid.RemoveAllChildren();

            _constructionView.RecipesGridScrollContainer.Visible = _constructionView.GridViewButtonPressed;
            _constructionView.Recipes.Visible = !_constructionView.GridViewButtonPressed;

            if (_constructionView.GridViewButtonPressed)
            {
                recipesList.PopulateList([]);
                PopulateGrid(recipesGrid, actualRecipes);
            }
            else
            {
                recipesList.PopulateList(actualRecipes);
            }
        }

        private void PopulateGrid(GridContainer recipesGrid,
            IEnumerable<ConstructionMenu.ConstructionMenuListData> actualRecipes)
        {
            foreach (var recipe in actualRecipes)
            {
                var protoView = new EntityPrototypeView()
                {
                    Scale = new Vector2(1.2f),
                };
                protoView.SetPrototype(recipe.TargetPrototype);

                var itemButton = new ContainerButton()
                {
                    VerticalAlignment = Control.VAlignment.Center,
                    Name = recipe.Prototype.Name,
                    ToolTip = recipe.Prototype.Name,
                    ToggleMode = true,
                    Children = { protoView },
                };

                var itemButtonPanelContainer = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = StyleNano.ButtonColorDefault },
                    Children = { itemButton },
                };

                itemButton.OnToggled += buttonToggledEventArgs =>
                {
                    SelectGridButton(itemButton, buttonToggledEventArgs.Pressed);

                    if (buttonToggledEventArgs.Pressed &&
                        _selected != null &&
                        _recipeButtons.TryGetValue(_selected.ID, out var oldButton))
                    {
                        oldButton.Pressed = false;
                        SelectGridButton(oldButton, false);
                    }

                    OnGridViewRecipeSelected(this, buttonToggledEventArgs.Pressed ? recipe.Prototype : null);
                };

                recipesGrid.AddChild(itemButtonPanelContainer);
                _recipeButtons[recipe.Prototype.ID] = itemButton;
                var isCurrentButtonSelected = _selected == recipe.Prototype;
                itemButton.Pressed = isCurrentButtonSelected;
                SelectGridButton(itemButton, isCurrentButtonSelected);
            }
        }

        private List<ConstructionMenu.ConstructionMenuListData> GetAndSortRecipes((string, string) args)
        {
            var recipes = new List<ConstructionMenu.ConstructionMenuListData>();

            var (search, category) = args;
            var isEmptyCategory = string.IsNullOrEmpty(category) || category == ForAllCategoryName;
            _selectedCategory = isEmptyCategory ? string.Empty : category;

            foreach (var recipe in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (recipe.Hide)
                    continue;

                if (_playerManager.LocalSession == null
                    || _playerManager.LocalEntity == null
                    || _whitelistSystem.IsWhitelistFail(recipe.EntityWhitelist, _playerManager.LocalEntity.Value))
                    continue;

                if (!string.IsNullOrEmpty(search) && (recipe.Name is { } name &&
                                                      !name.Contains(search.Trim(),
                                                          StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                if (!isEmptyCategory)
                {
                    if ((category != FavoriteCatName || !_favoritedRecipes.Contains(recipe)) &&
                        recipe.Category != category)
                        continue;
                }

                if (!_constructionSystem!.TryGetRecipePrototype(recipe.ID, out var targetProtoId))
                {
                    Logger.Error("Cannot find the target prototype in the recipe cache with the id \"{0}\" of {1}.",
                        recipe.ID,
                        nameof(ConstructionPrototype));
                    continue;
                }

                if (!_prototypeManager.TryIndex(targetProtoId, out EntityPrototype? proto))
                    continue;

                recipes.Add(new(recipe, proto));
            }

            recipes.Sort(
                (a, b) => string.Compare(a.Prototype.Name, b.Prototype.Name, StringComparison.InvariantCulture));

            return recipes;
        }

        private void SelectGridButton(BaseButton button, bool select)
        {
            if (button.Parent is not PanelContainer buttonPanel)
                return;

            button.Children.Single().Modulate = select ? Color.Green : Color.White;
            var buttonColor = select ? StyleNano.ButtonColorDefault : Color.Transparent;
            buttonPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = buttonColor };
        }

        private void PopulateCategories(string? selectCategory = null)
        {
            var uniqueCategories = new HashSet<string>();

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                var category = prototype.Category;

                if (!string.IsNullOrEmpty(category))
                    uniqueCategories.Add(category);
            }

            var isFavorites = _favoritedRecipes.Count > 0;
            var categoriesArray = new string[isFavorites ? uniqueCategories.Count + 2 : uniqueCategories.Count + 1];

            // hard-coded to show all recipes
            var idx = 0;
            categoriesArray[idx++] = ForAllCategoryName;

            // hard-coded to show favorites if it need
            if (isFavorites)
            {
                categoriesArray[idx++] = FavoriteCatName;
            }

            var sortedProtoCategories = uniqueCategories.OrderBy(Loc.GetString);

            foreach (var cat in sortedProtoCategories)
            {
                categoriesArray[idx++] = cat;
            }

            _constructionView.OptionCategories.Clear();

            for (var i = 0; i < categoriesArray.Length; i++)
            {
                _constructionView.OptionCategories.AddItem(Loc.GetString(categoriesArray[i]), i);

                if (!string.IsNullOrEmpty(selectCategory) && selectCategory == categoriesArray[i])
                    _constructionView.OptionCategories.SelectId(i);
            }

            _constructionView.Categories = categoriesArray;
        }

        private void PopulateInfo(ConstructionPrototype? prototype)
        {
            if (_constructionSystem is null)
                return;

            _constructionView.ClearRecipeInfo();

            if (prototype is null)
                return;

            if (!_constructionSystem.TryGetRecipePrototype(prototype.ID, out var targetProtoId))
                return;

            if (!_prototypeManager.TryIndex(targetProtoId, out EntityPrototype? proto))
                return;

            _constructionView.SetRecipeInfo(
                prototype.Name!,
                prototype.Description!,
                proto,
                prototype.Type != ConstructionType.Item,
                !_favoritedRecipes.Contains(prototype));

            var stepList = _constructionView.RecipeStepList;
            GenerateStepList(prototype, stepList);
        }

        private void GenerateStepList(ConstructionPrototype prototype, ItemList stepList)
        {
            if (_constructionSystem?.GetGuide(prototype) is not { } guide)
                return;

            foreach (var entry in guide.Entries)
            {
                var text = entry.Arguments != null
                    ? Loc.GetString(entry.Localization, entry.Arguments)
                    : Loc.GetString(entry.Localization);

                if (entry.EntryNumber is { } number)
                {
                    text = Loc.GetString("construction-presenter-step-wrapper",
                        ("step-number", number),
                        ("text", text));
                }

                // The padding needs to be applied regardless of text length... (See PadLeft documentation)
                text = text.PadLeft(text.Length + entry.Padding);

                var icon = entry.Icon != null ? _spriteSystem.Frame0(entry.Icon) : Texture.Transparent;
                stepList.AddItem(text, icon, false);
            }
        }

        private void BuildButtonToggled(bool pressed)
        {
            if (pressed)
            {
                if (_selected == null)
                    return;

                // not bound to a construction system
                if (_constructionSystem is null)
                {
                    _constructionView.BuildButtonPressed = false;
                    return;
                }

                if (_selected.Type == ConstructionType.Item)
                {
                    _constructionSystem.TryStartItemConstruction(_selected.ID);
                    _constructionView.BuildButtonPressed = false;
                    return;
                }

                _placementManager.BeginPlacing(new PlacementInformation
                    {
                        IsTile = false,
                        PlacementOption = _selected.PlacementMode
                    },
                    new ConstructionPlacementHijack(_constructionSystem, _selected));

                UpdateGhostPlacement();
            }
            else
                _placementManager.Clear();

            _constructionView.BuildButtonPressed = pressed;
        }

        private void UpdateGhostPlacement()
        {
            if (_selected == null)
                return;

            if (_selected.Type != ConstructionType.Structure)
            {
                _placementManager.Clear();
                return;
            }

            var constructSystem = _systemManager.GetEntitySystem<ConstructionSystem>();

            _placementManager.BeginPlacing(new PlacementInformation()
                {
                    IsTile = false,
                    PlacementOption = _selected.PlacementMode,
                },
                new ConstructionPlacementHijack(constructSystem, _selected));

            _constructionView.BuildButtonPressed = true;
        }

        private void OnSystemLoaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem system)
                SystemBindingChanged(system);
        }

        private void OnSystemUnloaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem)
                SystemBindingChanged(null);
        }

        private void OnViewFavoriteRecipe()
        {
            if (_selected is null)
                return;

            if (!_favoritedRecipes.Remove(_selected))
                _favoritedRecipes.Add(_selected);

            if (_selectedCategory == FavoriteCatName)
            {
                OnViewPopulateRecipes(_constructionView,
                    _favoritedRecipes.Count > 0 ? (string.Empty, FavoriteCatName) : (string.Empty, string.Empty));
            }

            var newFavorites = new List<ProtoId<ConstructionPrototype>>(_favoritedRecipes.Count);
            foreach (var recipe in _favoritedRecipes)
                newFavorites.Add(recipe.ID);

            _preferencesManager.UpdateConstructionFavorites(newFavorites);
            PopulateInfo(_selected);
            PopulateCategories(_selectedCategory);
        }

        public void SetFavorites(IReadOnlyList<ProtoId<ConstructionPrototype>> favorites)
        {
            _favoritedRecipes.Clear();

            foreach (var id in favorites)
            {
                if (_prototypeManager.TryIndex(id, out ConstructionPrototype? recipe, logError: false))
                    _favoritedRecipes.Add(recipe);
            }

            if (_selectedCategory == FavoriteCatName)
            {
                OnViewPopulateRecipes(_constructionView,
                    _favoritedRecipes.Count > 0 ? (string.Empty, FavoriteCatName) : (string.Empty, string.Empty));
            }

            PopulateCategories(_selectedCategory);
        }

        private void SystemBindingChanged(ConstructionSystem? newSystem)
        {
            if (newSystem is null)
            {
                if (_constructionSystem is null)
                    return;

                UnbindFromSystem();
            }
            else
            {
                if (_constructionSystem is null)
                {
                    BindToSystem(newSystem);
                    return;
                }

                UnbindFromSystem();
                BindToSystem(newSystem);
            }
        }

        private void BindToSystem(ConstructionSystem system)
        {
            _constructionSystem = system;

            OnViewPopulateRecipes(_constructionView, (string.Empty, string.Empty));

            system.ToggleCraftingWindow += SystemOnToggleMenu;
            system.FlipConstructionPrototype += SystemFlipConstructionPrototype;
            system.CraftingAvailabilityChanged += SystemCraftingAvailabilityChanged;
            system.ConstructionGuideAvailable += SystemGuideAvailable;
            if (_uiManager.GetActiveUIWidgetOrNull<GameTopMenuBar>() != null)
            {
                CraftingAvailable = system.CraftingEnabled;
            }
        }

        private void UnbindFromSystem()
        {
            var system = _constructionSystem;

            if (system is null)
                throw new InvalidOperationException();

            system.ToggleCraftingWindow -= SystemOnToggleMenu;
            system.FlipConstructionPrototype -= SystemFlipConstructionPrototype;
            system.CraftingAvailabilityChanged -= SystemCraftingAvailabilityChanged;
            system.ConstructionGuideAvailable -= SystemGuideAvailable;
            _constructionSystem = null;
        }

        private void SystemCraftingAvailabilityChanged(object? sender, CraftingAvailabilityChangedArgs e)
        {
            if (_uiManager.ActiveScreen == null)
                return;
            CraftingAvailable = e.Available;
        }

        private void SystemOnToggleMenu(object? sender, EventArgs eventArgs)
        {
            if (!CraftingAvailable)
                return;

            if (WindowOpen)
            {
                if (IsAtFront)
                {
                    WindowOpen = false;
                    _uiManager.GetActiveUIWidget<GameTopMenuBar>()
                        .CraftingButton.SetClickPressed(false); // This does not call CraftingButtonToggled
                }
                else
                    _constructionView.MoveToFront();
            }
            else
            {
                WindowOpen = true;
                _uiManager.GetActiveUIWidget<GameTopMenuBar>()
                    .CraftingButton.SetClickPressed(true); // This does not call CraftingButtonToggled
            }
        }

        private void SystemFlipConstructionPrototype(object? sender, EventArgs eventArgs)
        {
            if (!_placementManager.IsActive || _placementManager.Eraser)
            {
                return;
            }

            if (_selected == null || _selected.Mirror == null)
            {
                return;
            }

            _selected = _prototypeManager.Index<ConstructionPrototype>(_selected.Mirror);
            UpdateGhostPlacement();
        }

        private void SystemGuideAvailable(object? sender, string e)
        {
            if (!CraftingAvailable)
                return;

            if (!WindowOpen)
                return;

            if (_selected == null)
                return;

            PopulateInfo(_selected);
        }
    }
}
