using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction
{
    public class ConstructionMenu : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;

        private readonly Button BuildButton;
        private readonly Button EraseButton;
        private readonly LineEdit SearchBar;
        private readonly Tree RecipeList;
        private readonly TextureRect InfoIcon;
        private readonly Label InfoLabel;
        private readonly ItemList StepList;

        private CategoryNode RootCategory;

        // This list is flattened in such a way that the top most deepest category is first.
        private List<CategoryNode> FlattenedCategories;
        private readonly PlacementManager Placement;

        protected override Vector2? CustomSize => (500, 350);

        public ConstructionMenu()
        {
            IoCManager.InjectDependencies(this);
            Placement = (PlacementManager) IoCManager.Resolve<IPlacementManager>();
            Placement.PlacementChanged += OnPlacementChanged;

            Title = "Construction";

            var hSplitContainer = new HSplitContainer();

            // Left side
            var recipes = new VBoxContainer {CustomMinimumSize = new Vector2(150.0f, 0.0f)};
            SearchBar = new LineEdit {PlaceHolder = "Search"};
            RecipeList = new Tree {SizeFlagsVertical = SizeFlags.FillExpand, HideRoot = true};
            recipes.AddChild(SearchBar);
            recipes.AddChild(RecipeList);
            hSplitContainer.AddChild(recipes);

            // Right side
            var guide = new VBoxContainer();
            var info = new HBoxContainer();
            InfoIcon = new TextureRect();
            InfoLabel = new Label
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            info.AddChild(InfoIcon);
            info.AddChild(InfoLabel);
            guide.AddChild(info);

            var stepsLabel = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Text = "Steps"
            };
            guide.AddChild(stepsLabel);

            StepList = new ItemList
            {
                SizeFlagsVertical = SizeFlags.FillExpand, SelectMode = ItemList.ItemListSelectMode.None
            };
            guide.AddChild(StepList);

            var buttonsContainer = new HBoxContainer();
            BuildButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                TextAlign = Label.AlignMode.Center,
                Text = "Build!",
                Disabled = true,
                ToggleMode = true
            };
            EraseButton = new Button
            {
                TextAlign = Label.AlignMode.Center, Text = "Clear Ghosts", ToggleMode = true
            };
            buttonsContainer.AddChild(BuildButton);
            buttonsContainer.AddChild(EraseButton);
            guide.AddChild(buttonsContainer);

            hSplitContainer.AddChild(guide);
            Contents.AddChild(hSplitContainer);

            BuildButton.OnToggled += OnBuildToggled;
            EraseButton.OnToggled += OnEraseToggled;
            SearchBar.OnTextChanged += OnTextEntered;
            RecipeList.OnItemSelected += OnItemSelected;

            PopulatePrototypeList();
            PopulateTree();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Placement.PlacementChanged -= OnPlacementChanged;
            }
        }

        private void OnItemSelected()
        {
            var prototype = (ConstructionPrototype) RecipeList.Selected.Metadata;

            if (prototype == null)
            {
                InfoLabel.Text = "";
                InfoIcon.Texture = null;
                StepList.Clear();
                BuildButton.Disabled = true;
            }
            else
            {
                BuildButton.Disabled = false;
                InfoLabel.Text = prototype.Description;
                InfoIcon.Texture = prototype.Icon.Frame0();

                StepList.Clear();

                foreach (var forward in prototype.Stages.Select(a => a.Forward))
                {
                    if (forward == null)
                    {
                        continue;
                    }

                    Texture icon;
                    string text;
                    switch (forward)
                    {
                        case ConstructionStepMaterial mat:
                            switch (mat.Material)
                            {
                                case ConstructionStepMaterial.MaterialType.Metal:
                                    icon = _resourceCache.GetResource<TextureResource>(
                                        "/Textures/Objects/Materials/sheet_metal.png");
                                    text = $"Metal x{mat.Amount}";
                                    break;
                                case ConstructionStepMaterial.MaterialType.Glass:
                                    icon = _resourceCache.GetResource<TextureResource>(
                                        "/Textures/Objects/Materials/sheet_glass.png");
                                    text = $"Glass x{mat.Amount}";
                                    break;
                                case ConstructionStepMaterial.MaterialType.Cable:
                                    icon = _resourceCache.GetResource<TextureResource>(
                                        "/Textures/Objects/Tools/cable_coil.png");
                                    text = $"Cable Coil x{mat.Amount}";
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            break;
                        case ConstructionStepTool tool:
                            switch (tool.ToolQuality)
                            {
                                case ToolQuality.Anchoring:
                                    icon = _resourceCache.GetResource<TextureResource>("/Textures/Objects/Tools/wrench.rsi/icon.png");
                                    text = "Wrench";
                                    break;
                                case ToolQuality.Prying:
                                    icon = _resourceCache.GetResource<TextureResource>("/Textures/Objects/Tools/crowbar.rsi/icon.png");
                                    text = "Crowbar";
                                    break;
                                case ToolQuality.Screwing:
                                    icon = _resourceCache.GetResource<TextureResource>(
                                        "/Textures/Objects/Tools/screwdriver.rsi/screwdriver-map.png");
                                    text = "Screwdriver";
                                    break;
                                case ToolQuality.Welding:
                                    icon = _resourceCache.GetResource<RSIResource>("/Textures/Objects/tools.rsi")
                                        .RSI["welder"].Frame0;
                                    text = $"Welding tool ({tool.Amount} fuel)";
                                    break;
                                case ToolQuality.Cutting:
                                    icon = _resourceCache.GetResource<TextureResource>(
                                        "/Textures/Objects/Tools/wirecutters.rsi/cutters-map.png");
                                    text = "Wirecutters";
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    StepList.AddItem(text, icon, false);
                }
            }
        }

        private void OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            var str = args.Text;
            PopulateTree(string.IsNullOrWhiteSpace(str) ? null : str.ToLowerInvariant());
        }

        private void OnBuildToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed)
            {
                var prototype = (ConstructionPrototype) RecipeList.Selected.Metadata;
                if (prototype == null)
                {
                    return;
                }

                if (prototype.Type == ConstructionType.Item)
                {
                    var constructSystem = _systemManager.GetEntitySystem<ConstructionSystem>();
                    constructSystem.TryStartItemConstruction(prototype.ID);
                    BuildButton.Pressed = false;
                    return;
                }

                Placement.BeginHijackedPlacing(
                    new PlacementInformation
                    {
                        IsTile = false,
                        PlacementOption = prototype.PlacementMode
                    },
                    new ConstructionPlacementHijack(_systemManager.GetEntitySystem<ConstructionSystem>(), prototype));
            }
            else
            {
                Placement.Clear();
            }
            BuildButton.Pressed = args.Pressed;
        }

        private void OnEraseToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed) Placement.Clear();
            Placement.ToggleEraserHijacked(new ConstructionPlacementHijack(_systemManager.GetEntitySystem<ConstructionSystem>(), null));
            EraseButton.Pressed = args.Pressed;
        }

        private void OnPlacementChanged(object sender, EventArgs e)
        {
            BuildButton.Pressed = false;
            EraseButton.Pressed = false;
        }

        private void PopulatePrototypeList()
        {
            RootCategory = new CategoryNode("", null);
            var count = 1;

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                var currentNode = RootCategory;

                foreach (var category in prototype.CategorySegments)
                {
                    if (!currentNode.ChildCategories.TryGetValue(category, out var subNode))
                    {
                        count++;
                        subNode = new CategoryNode(category, currentNode);
                        currentNode.ChildCategories.Add(category, subNode);
                    }

                    currentNode = subNode;
                }

                currentNode.Prototypes.Add(prototype);
            }

            // Do a pass to sort the prototype lists and flatten the hierarchy.
            void Recurse(CategoryNode node)
            {
                // I give up we're using recursion to flatten this.
                // There probably IS a way to do it.
                // I'm too stupid to think of what that way is.
                foreach (var child in node.ChildCategories.Values)
                {
                    Recurse(child);
                }

                node.Prototypes.Sort(ComparePrototype);
                FlattenedCategories.Add(node);
                node.FlattenedIndex = FlattenedCategories.Count - 1;
            }

            FlattenedCategories = new List<CategoryNode>(count);
            Recurse(RootCategory);
        }

        private void PopulateTree(string searchTerm = null)
        {
            RecipeList.Clear();

            var categoryItems = new Tree.Item[FlattenedCategories.Count];
            categoryItems[RootCategory.FlattenedIndex] = RecipeList.CreateItem();

            // Yay more recursion.
            Tree.Item ItemForNode(CategoryNode node)
            {
                if (categoryItems[node.FlattenedIndex] != null)
                {
                    return categoryItems[node.FlattenedIndex];
                }

                var item = RecipeList.CreateItem(ItemForNode(node.Parent));
                item.Text = node.Name;
                item.Selectable = false;
                categoryItems[node.FlattenedIndex] = item;
                return item;
            }

            foreach (var node in FlattenedCategories)
            {
                foreach (var prototype in node.Prototypes)
                {
                    if (searchTerm != null)
                    {
                        var found = false;
                        // TODO: don't run ToLowerInvariant() constantly.
                        if (prototype.Name.ToLowerInvariant().IndexOf(searchTerm, StringComparison.Ordinal) != -1)
                        {
                            found = true;
                        }
                        else
                        {
                            foreach (var keyw in prototype.Keywords.Concat(prototype.CategorySegments))
                            {
                                // TODO: don't run ToLowerInvariant() constantly.
                                if (keyw.ToLowerInvariant().IndexOf(searchTerm, StringComparison.Ordinal) != -1)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            continue;
                        }
                    }

                    var subItem = RecipeList.CreateItem(ItemForNode(node));
                    subItem.Text = prototype.Name;
                    subItem.Metadata = prototype;
                }
            }
        }

        private static int ComparePrototype(ConstructionPrototype x, ConstructionPrototype y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        private class CategoryNode
        {
            public readonly string Name;
            public readonly CategoryNode Parent;

            public readonly SortedDictionary<string, CategoryNode>
                ChildCategories = new SortedDictionary<string, CategoryNode>();

            public readonly List<ConstructionPrototype> Prototypes = new List<ConstructionPrototype>();
            public int FlattenedIndex = -1;

            public CategoryNode(string name, CategoryNode parent)
            {
                Name = name;
                Parent = parent;
            }
        }
    }
}
