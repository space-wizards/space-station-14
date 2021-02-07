using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

#nullable enable

namespace Content.Client.Construction
{
    /// <summary>
    /// This class presents the Construction/Crafting UI to the client, linking the <see cref="ConstructionSystem"/> with a <see cref="ConstructionMenu"/>.
    /// </summary>
    internal class ConstructionMenuPresenter : IDisposable
    {
        private readonly IEntitySystemManager _systemManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IResourceCache _resourceCache;
        private readonly IPlacementManager _placementManager;

        private readonly IGameHud _gameHud;
        private readonly IConstructionMenuView _constructionView;
        private ConstructionSystem? _constructionSystem;
        
        private ConstructionPrototype? _selected;

        private bool CraftingAvailable
        {
            get => _gameHud.CraftingButtonVisible;
            set
            {
                _gameHud.CraftingButtonVisible = value;
                if(!value)
                    _constructionView.Close();
            }
        }

        private bool WindowOpen
        {
            get => _constructionView.IsOpen;
            set
            {
                if(value && CraftingAvailable)
                {
                    if(_constructionView.IsOpen)
                        _constructionView.MoveToFront();
                    else
                        _constructionView.OpenCentered();
                }
                else
                    _constructionView.Close();
            }
        }

        /// <summary>
        /// Does the window have focus? If the window is closed, this will always return false.
        /// </summary>
        private bool IsAtFront => _constructionView.IsOpen && _constructionView.IsAtFront();

        /// <summary>
        /// Constructs a new instance of <see cref="ConstructionMenuPresenter"/>.
        /// </summary>
        /// <param name="gameHud">GUI that is being presented to.</param>
        /// <param name="systemManager">EntitySystem that contains a ConstructionSystem being presented from.</param>
        /// <param name="prototypeManager"></param>
        /// <param name="resourceCache"></param>
        /// <param name="placementManager"></param>
        public ConstructionMenuPresenter(IGameHud gameHud,
            IEntitySystemManager systemManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache,
            IPlacementManager placementManager)
        {
            _gameHud = gameHud;
            _systemManager = systemManager;
            _prototypeManager = prototypeManager;
            _resourceCache = resourceCache;
            _placementManager = placementManager;

            // This is required so that if we load after the system is initialized
            if (_systemManager.TryGetEntitySystem<ConstructionSystem>(out var constructionSystem))
                SystemBindingChanged(constructionSystem);

            _systemManager.SystemLoaded += OnSystemLoaded;
            _systemManager.SystemUnloaded += OnSystemUnloaded;

            _placementManager.PlacementChanged += OnPlacementChanged;

            _constructionView = new ConstructionMenu();
            _constructionView.OnClose += () => _gameHud.CraftingButtonDown = false;
            _constructionView.ClearAllGhosts += (_, _) => _constructionSystem?.ClearAllGhosts();
            _constructionView.PopulateRecipes += OnViewPopulateRecipes;
            _constructionView.RecipeSelected += OnViewRecipeSelected;
            _constructionView.BuildButtonToggled += (_, b) => BuildButtonToggled(b);
            _constructionView.EraseButtonToggled += (_, b) =>
            {
                if (b) _placementManager.Clear();
                _placementManager.ToggleEraserHijacked(new ConstructionPlacementHijack(_systemManager.GetEntitySystem<ConstructionSystem>(), null));
                _constructionView.EraseButtonPressed = b;
            };

            PopulateCategories(_constructionView, _prototypeManager);
            OnViewPopulateRecipes(_constructionView, (string.Empty, string.Empty));

            _gameHud.CraftingButtonToggled += b => WindowOpen = b;
        }

        private void OnPlacementChanged(object? sender, EventArgs e)
        {
            _constructionView.ResetPlacement();
        }

        private void OnViewRecipeSelected(object? sender, ItemList.Item? item)
        {
            if (item is null)
            {
                _selected = null;
                _constructionView.ClearInfo();
                return;
            }

            _selected = (ConstructionPrototype) item.Metadata!;
            PopulateInfo(_constructionView, _selected, _prototypeManager);
        }

        private void OnViewPopulateRecipes(object? sender, (string search, string catagory) args)
        {
            var (search, category) = args;
            var RecipesList = _constructionView.Recipes;

            RecipesList.Clear();
            
            foreach (var recipe in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (!string.IsNullOrEmpty(search))
                {
                    if (!recipe.Name.ToLowerInvariant().Contains(search.Trim().ToLowerInvariant()))
                        continue;
                }

                if (!string.IsNullOrEmpty(category) && category != Loc.GetString("All"))
                {
                    if (recipe.Category != category)
                        continue;
                }

                RecipesList.Add(GetItem(recipe, RecipesList));
            }
        }

        private void PopulateCategories(IConstructionMenuView constructionMenu, IPrototypeManager prototypeManager)
        {
            var uniqueCategories = new HashSet<string>();

            // hard-coded to show all recipes
            uniqueCategories.Add(Loc.GetString("All"));

            foreach (var prototype in prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                var category = Loc.GetString(prototype.Category);

                if (!string.IsNullOrEmpty(category))
                    uniqueCategories.Add(category);
            }

            constructionMenu.CategoryButton.Clear();

            var array = uniqueCategories.ToArray();
            Array.Sort(array);

            for (var i = 0; i < array.Length; i++)
            {
                var category = array[i];
                constructionMenu.CategoryButton.AddItem(category, i);
            }

            constructionMenu.Categories = array;
        }

        private void BuildButtonToggled(bool pressed)
        {
            if (pressed)
            {
                if (_selected == null) return;

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

                _placementManager.BeginPlacing(new PlacementInformation()
                {
                    IsTile = false,
                    PlacementOption = _selected.PlacementMode,
                }, new ConstructionPlacementHijack(_constructionSystem, _selected));
            }
            else
            {
                _placementManager.Clear();
            }

            _constructionView.BuildButtonPressed = pressed;
        }

        private void OnSystemLoaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem system)
            {
                SystemBindingChanged(system);
            }
        }

        private void OnSystemUnloaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem)
            {
                SystemBindingChanged(null);
            }
        }

        private void SystemBindingChanged(ConstructionSystem? newSystem)
        {
            if (newSystem is null)
            {
                if(_constructionSystem is null)
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

                //TODO: update the view
            }
        }

        private void BindToSystem(ConstructionSystem system)
        {
            _constructionSystem = system;
            system.ToggleCraftingWindow += SystemOnToggleMenu;
            system.CraftingAvailabilityChanged += SystemCraftingAvailabilityChanged;
        }

        private void UnbindFromSystem()
        {
            var system = _constructionSystem;

            if(system is null)
                throw new InvalidOperationException();

            system.ToggleCraftingWindow -= SystemOnToggleMenu;
            system.CraftingAvailabilityChanged -= SystemCraftingAvailabilityChanged;
            _constructionSystem = null;
        }

        private void SystemCraftingAvailabilityChanged(object? sender, CraftingAvailabilityChangedArgs e)
        {
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
                    _gameHud.CraftingButtonDown = false; // This does not call CraftingButtonToggled
                }
                else
                {
                    _constructionView.MoveToFront();
                }
            }
            else
            {
                WindowOpen = true;
                _gameHud.CraftingButtonDown = true; // This does not call CraftingButtonToggled
            }
        }

        private void PopulateInfo(IConstructionMenuView constructionMenu, ConstructionPrototype prototype, IPrototypeManager PrototypeManager)
        {
            constructionMenu.ClearInfo();
            constructionMenu.SetInfo(prototype.Name, prototype.Description, prototype.Icon.Frame0(), prototype.Type != ConstructionType.Item);

            var stepList = constructionMenu.RecipeStepList;

            if (!PrototypeManager.TryIndex(prototype.Graph, out ConstructionGraphPrototype graph))
                return;

            var startNode = graph.Nodes[prototype.StartNode];
            var targetNode = graph.Nodes[prototype.TargetNode];

            var path = graph.Path(startNode.Name, targetNode.Name);

            var current = startNode;

            var stepNumber = 1;

            foreach (var node in path)
            {
                var edge = current.GetEdge(node.Name);
                var firstNode = current == startNode;

                if (firstNode)
                {
                    stepList.AddItem(prototype.Type == ConstructionType.Item
                        ? Loc.GetString($"{stepNumber++}. To craft this item, you need:")
                        : Loc.GetString($"{stepNumber++}. To build this, first you need:"));
                }

                foreach (var step in edge.Steps)
                {
                    var icon = GetTextureForStep(_resourceCache, step);

                    switch (step)
                    {
                        case MaterialConstructionGraphStep materialStep:
                            stepList.AddItem(
                                !firstNode
                                    ? Loc.GetString(
                                        "{0}. Add {1}x {2}.", stepNumber++, materialStep.Amount, materialStep.Material)
                                    : Loc.GetString("      {0}x {1}", materialStep.Amount, materialStep.Material), icon);

                            break;

                        case ToolConstructionGraphStep toolStep:
                            stepList.AddItem(Loc.GetString("{0}. Use a {1}.", stepNumber++, toolStep.Tool.GetToolName()), icon);
                            break;

                        case PrototypeConstructionGraphStep prototypeStep:
                            stepList.AddItem(Loc.GetString("{0}. Add {1}.", stepNumber++, prototypeStep.Name), icon);
                            break;

                        case ComponentConstructionGraphStep componentStep:
                            stepList.AddItem(Loc.GetString("{0}. Add {1}.", stepNumber++, componentStep.Name), icon);
                            break;

                        case NestedConstructionGraphStep nestedStep:
                            var parallelNumber = 1;
                            stepList.AddItem(Loc.GetString("{0}. In parallel...", stepNumber++));

                            foreach (var steps in nestedStep.Steps)
                            {
                                var subStepNumber = 1;

                                foreach (var subStep in steps)
                                {
                                    icon = GetTextureForStep(_resourceCache, subStep);

                                    switch (subStep)
                                    {
                                        case MaterialConstructionGraphStep materialStep:
                                            if (!(prototype.Type == ConstructionType.Item)) stepList.AddItem(Loc.GetString("    {0}.{1}.{2}. Add {3}x {4}.", stepNumber, parallelNumber, subStepNumber++, materialStep.Amount, materialStep.Material), icon);
                                            break;

                                        case ToolConstructionGraphStep toolStep:
                                            stepList.AddItem(Loc.GetString("    {0}.{1}.{2}. Use a {3}.", stepNumber, parallelNumber, subStepNumber++, toolStep.Tool.GetToolName()), icon);
                                            break;

                                        case PrototypeConstructionGraphStep prototypeStep:
                                            stepList.AddItem(Loc.GetString("    {0}.{1}.{2}. Add {3}.", stepNumber, parallelNumber, subStepNumber++, prototypeStep.Name), icon);
                                            break;

                                        case ComponentConstructionGraphStep componentStep:
                                            stepList.AddItem(Loc.GetString("    {0}.{1}.{2}. Add {3}.", stepNumber, parallelNumber, subStepNumber++, componentStep.Name), icon);
                                            break;
                                    }
                                }

                                parallelNumber++;
                            }

                            break;
                    }
                }

                current = node;
            }
        }
        
        private static Texture? GetTextureForStep(IResourceCache resourceCache, ConstructionGraphStep step)
        {
            switch (step)
            {
                case MaterialConstructionGraphStep materialStep:
                    switch (materialStep.Material)
                    {
                        case StackType.Metal:
                            return resourceCache.GetTexture("/Textures/Objects/Materials/sheets.rsi/metal.png");

                        case StackType.Glass:
                            return resourceCache.GetTexture("/Textures/Objects/Materials/sheets.rsi/glass.png");

                        case StackType.Plasteel:
                            return resourceCache.GetTexture("/Textures/Objects/Materials/sheets.rsi/plasteel.png");

                        case StackType.Phoron:
                            return resourceCache.GetTexture("/Textures/Objects/Materials/sheets.rsi/phoron.png");

                        case StackType.Cable:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/cables.rsi/coil-30.png");

                        case StackType.MetalRod:
                            return resourceCache.GetTexture("/Textures/Objects/Materials/materials.rsi/rods.png");

                    }
                    break;

                case ToolConstructionGraphStep toolStep:
                    switch (toolStep.Tool)
                    {
                        case ToolQuality.Anchoring:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/wrench.rsi/icon.png");
                        case ToolQuality.Prying:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/crowbar.rsi/icon.png");
                        case ToolQuality.Screwing:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/screwdriver.rsi/screwdriver-map.png");
                        case ToolQuality.Cutting:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/wirecutters.rsi/cutters-map.png");
                        case ToolQuality.Welding:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/welder.rsi/welder.png");
                        case ToolQuality.Multitool:
                            return resourceCache.GetTexture("/Textures/Objects/Tools/multitool.rsi/multitool.png");
                    }

                    break;

                case ComponentConstructionGraphStep componentStep:
                    return componentStep.Icon?.Frame0();

                case PrototypeConstructionGraphStep prototypeStep:
                    return prototypeStep.Icon?.Frame0();

                case NestedConstructionGraphStep:
                    return null;
            }

            return null;
        }

        private static ItemList.Item GetItem(ConstructionPrototype recipe, ItemList itemList)
        {
            return new(itemList)
            {
                Metadata = recipe,
                Text = recipe.Name,
                Icon = recipe.Icon.Frame0(),
                TooltipEnabled = true,
                TooltipText = recipe.Description,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _constructionView.Dispose();

            _systemManager.SystemLoaded -= OnSystemLoaded;
            _systemManager.SystemUnloaded -= OnSystemUnloaded;
            
            _placementManager.PlacementChanged -= OnPlacementChanged;
        }
    }
}
