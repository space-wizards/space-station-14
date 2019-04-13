using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Construction;
using Content.Shared.Construction;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Placement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.Utility;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;

namespace Content.Client.Construction
{
    public class ConstructionMenu : SS14Window
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Construction/ConstructionMenu.tscn");

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
        public ConstructionMenu(IDisplayManager displayMan) : base(displayMan) { }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            Placement = (PlacementManager)IoCManager.Resolve<IPlacementManager>();
            Placement.PlacementCanceled += OnPlacementCanceled;

            HideOnClose = true;
            Title = "Construction";
            Visible = false;
            var split = Contents.GetChild("HSplitContainer");
            var rightSide = split.GetChild("Guide");
            var info = rightSide.GetChild("Info");
            InfoIcon = info.GetChild<TextureRect>("TextureRect");
            InfoLabel = info.GetChild<Label>("Label");
            StepList = rightSide.GetChild<ItemList>("StepsList");
            var buttons = rightSide.GetChild("Buttons");
            BuildButton = buttons.GetChild<Button>("BuildButton");
            BuildButton.OnPressed += OnBuildPressed;
            EraseButton = buttons.GetChild<Button>("EraseButton");
            EraseButton.OnToggled += OnEraseToggled;

            var leftSide = split.GetChild("Recipes");
            SearchBar = leftSide.GetChild<LineEdit>("Search");
            SearchBar.OnTextChanged += OnTextEntered;
            RecipeList = leftSide.GetChild<Tree>("Tree");
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
                item.SetText(0, node.Name);
                item.SetSelectable(0, false);
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
                    subItem.SetText(0, prototype.Name);
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
