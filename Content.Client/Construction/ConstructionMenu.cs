using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Construction;
using Content.Shared.Construction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Construction
{
    public class ConstructionMenu : SS14Window
    {

#pragma warning disable CS0649
        [Dependency]
        readonly IPrototypeManager PrototypeManager;
        [Dependency]
        readonly IResourceCache ResourceCache;
#pragma warning restore

        public ConstructorComponent Owner { get; set; }
        Button BuildButton;
        Button EraseButton;
        LineEdit SearchBar;
        Tree RecipeList;
        TextureRect InfoIcon;
        Label InfoLabel;
        ItemList StepList;

        CategoryNode RootCategory;
        // This list is flattened in such a way that the top most deepest category is first.
        List<CategoryNode> FlattenedCategories;
        PlacementManager Placement;

        public ConstructionMenu()
        {
            Size = new Vector2(500.0f, 350.0f);
        }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            Placement = (PlacementManager)IoCManager.Resolve<IPlacementManager>();
            Placement.PlacementCanceled += OnPlacementCanceled;

            HideOnClose = true;
            Title = "Construction";
            Visible = false;

            var hSplitContainer = new HSplitContainer();

            // Left side
            var recipes = new VBoxContainer("Recipes") {CustomMinimumSize = new Vector2(150.0f, 0.0f)};
            SearchBar = new LineEdit("Search") {PlaceHolder = "Search"};
            RecipeList = new Tree("Tree") {SizeFlagsVertical = SizeFlags.FillExpand, HideRoot = true};
            recipes.AddChild(SearchBar);
            recipes.AddChild(RecipeList);
            hSplitContainer.AddChild(recipes);

            // Right side
            var guide = new VBoxContainer("Guide");
            var info = new HBoxContainer("Info");
            InfoIcon = new TextureRect("TextureRect");
            InfoLabel = new Label("Label")
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            info.AddChild(InfoIcon);
            info.AddChild(InfoLabel);
            guide.AddChild(info);

            var stepsLabel = new Label("Label")
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Text = "Steps"
            };
            guide.AddChild(stepsLabel);

            StepList = new ItemList("StepsList")
            {
                SizeFlagsVertical = SizeFlags.FillExpand, SelectMode = ItemList.ItemListSelectMode.None
            };
            guide.AddChild(StepList);

            var buttonsContainer = new HBoxContainer("Buttons");
            BuildButton = new Button("BuildButton")
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                TextAlign = Button.AlignMode.Center,
                Text = "Build!",
                Disabled = true,
                ToggleMode = false
            };
            EraseButton = new Button("EraseButton")
            {
                TextAlign = Button.AlignMode.Center, Text = "Clear Ghosts", ToggleMode = true
            };
            buttonsContainer.AddChild(BuildButton);
            buttonsContainer.AddChild(EraseButton);
            guide.AddChild(buttonsContainer);

            hSplitContainer.AddChild(guide);
            Contents.AddChild(hSplitContainer);

            BuildButton.OnPressed += OnBuildPressed;
            EraseButton.OnToggled += OnEraseToggled;
            SearchBar.OnTextChanged += OnTextEntered;
            RecipeList.OnItemSelected += OnItemSelected;

            PopulatePrototypeList();
            PopulateTree();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Placement.PlacementCanceled -= OnPlacementCanceled;
            }
        }

        void OnItemSelected()
        {
            var prototype = (ConstructionPrototype)RecipeList.Selected.Metadata;

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
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/sheet_metal.png");
                                    text = $"Metal x{mat.Amount}";
                                    break;
                                case ConstructionStepMaterial.MaterialType.Glass:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/sheet_glass.png");
                                    text = $"Glass x{mat.Amount}";
                                    break;
                                case ConstructionStepMaterial.MaterialType.Cable:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/cable_coil.png");
                                    text = $"Cable Coil x{mat.Amount}";
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            break;
                        case ConstructionStepTool tool:
                            switch (tool.Tool)
                            {
                                case ConstructionStepTool.ToolType.Wrench:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/wrench.png");
                                    text = "Wrench";
                                    break;
                                case ConstructionStepTool.ToolType.Crowbar:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png");
                                    text = "Crowbar";
                                    break;
                                case ConstructionStepTool.ToolType.Screwdriver:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/screwdriver.png");
                                    text = "Screwdriver";
                                    break;
                                case ConstructionStepTool.ToolType.Welder:
                                    icon = ResourceCache.GetResource<RSIResource>("/Textures/Objects/tools.rsi").RSI["welder"].Frame0;
                                    text = $"Welding tool ({tool.Amount} fuel)";
                                    break;
                                case ConstructionStepTool.ToolType.Wirecutters:
                                    icon = ResourceCache.GetResource<TextureResource>("/Textures/Objects/wirecutter.png");
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

        void OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            var str = args.Text;
            PopulateTree(string.IsNullOrWhiteSpace(str) ? null : str.ToLowerInvariant());
        }

        void OnBuildPressed(Button.ButtonEventArgs args)
        {
            var prototype = (ConstructionPrototype)RecipeList.Selected.Metadata;
            if (prototype == null)
            {
                return;
            }

            if (prototype.Type != ConstructionType.Structure)
            {
                // In-hand attackby doesn't exist so this is the best alternative.
                var loc = Owner.Owner.GetComponent<ITransformComponent>().GridPosition;
                Owner.SpawnGhost(prototype, loc, Direction.North);
                return;
            }

            var hijack = new ConstructionPlacementHijack(prototype, Owner);
            var info = new PlacementInformation
            {
                IsTile = false,
                PlacementOption = prototype.PlacementMode,
            };


            Placement.BeginHijackedPlacing(info, hijack);
        }

        private void OnEraseToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var hijack = new ConstructionPlacementHijack(null, Owner);
            Placement.ToggleEraserHijacked(hijack);
        }

        void PopulatePrototypeList()
        {
            RootCategory = new CategoryNode("", null);
            int count = 1;

            foreach (var prototype in PrototypeManager.EnumeratePrototypes<ConstructionPrototype>())
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

        void PopulateTree(string searchTerm = null)
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
                        if (prototype.Name.ToLowerInvariant().IndexOf(searchTerm) != -1)
                        {
                            found = true;
                        }
                        else
                        {
                            foreach (var keyw in prototype.Keywords.Concat(prototype.CategorySegments))
                            {
                                // TODO: don't run ToLowerInvariant() constantly.
                                if (keyw.ToLowerInvariant().IndexOf(searchTerm) != -1)
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

        private void OnPlacementCanceled(object sender, EventArgs e)
        {
            EraseButton.Pressed = false;
        }

        private static int ComparePrototype(ConstructionPrototype x, ConstructionPrototype y)
        {
            return x.Name.CompareTo(y.Name);
        }

        class CategoryNode
        {
            public readonly string Name;
            public readonly CategoryNode Parent;
            public SortedDictionary<string, CategoryNode> ChildCategories = new SortedDictionary<string, CategoryNode>();
            public List<ConstructionPrototype> Prototypes = new List<ConstructionPrototype>();
            public int FlattenedIndex = -1;

            public CategoryNode(string name, CategoryNode parent)
            {
                Name = name;
                Parent = parent;
            }
        }
    }
}
