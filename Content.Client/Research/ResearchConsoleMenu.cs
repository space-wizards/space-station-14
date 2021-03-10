using System.Collections.Generic;
using Content.Client.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Research
{
    public class ResearchConsoleMenu : SS14Window
    {
        public ResearchConsoleBoundUserInterface Owner { get; set; }

        private readonly List<TechnologyPrototype> _unlockedTechnologyPrototypes = new();
        private readonly List<TechnologyPrototype> _unlockableTechnologyPrototypes = new();
        private readonly List<TechnologyPrototype> _futureTechnologyPrototypes = new();

        private readonly Label _pointLabel;
        private readonly Label _pointsPerSecondLabel;
        private readonly Label _technologyName;
        private readonly Label _technologyDescription;
        private readonly Label _technologyRequirements;
        private readonly TextureRect _technologyIcon;
        private readonly ItemList _unlockedTechnologies;
        private readonly ItemList _unlockableTechnologies;
        private readonly ItemList _futureTechnologies;

        public Button UnlockButton { get; private set; }
        public Button ServerSelectionButton { get; private set; }
        public Button ServerSyncButton { get; private set; }

        public TechnologyPrototype TechnologySelected;

        public ResearchConsoleMenu(ResearchConsoleBoundUserInterface owner = null)
        {
            SetSize = MinSize = (800, 400);

            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("R&D Console");

            Owner = owner;

            _unlockedTechnologies = new ItemList()
            {
                SelectMode = ItemList.ItemListSelectMode.Button,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            _unlockedTechnologies.OnItemSelected += UnlockedTechnologySelected;

            _unlockableTechnologies = new ItemList()
            {
                SelectMode = ItemList.ItemListSelectMode.Button,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            _unlockableTechnologies.OnItemSelected += UnlockableTechnologySelected;

            _futureTechnologies = new ItemList()
            {
                SelectMode = ItemList.ItemListSelectMode.Button,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            _futureTechnologies.OnItemSelected += FutureTechnologySelected;

            var vbox = new VBoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            var hboxTechnologies = new HBoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 2,
                SeparationOverride = 10,
            };

            var hboxSelected = new HBoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1
            };

            var vboxPoints =  new VBoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1,
            };

            var vboxTechInfo = new VBoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 3,
            };

            _pointLabel = new Label() { Text = Loc.GetString("Research Points") + ": 0" };
            _pointsPerSecondLabel = new Label() { Text = Loc.GetString("Points per Second") + ": 0" };

            var vboxPointsButtons = new VBoxContainer()
            {
                Align = BoxContainer.AlignMode.End,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            ServerSelectionButton = new Button() { Text = Loc.GetString("Server list") };
            ServerSyncButton = new Button() { Text = Loc.GetString("Sync")};
            UnlockButton = new Button() { Text = Loc.GetString("Unlock"), Disabled = true };


            vboxPointsButtons.AddChild(ServerSelectionButton);
            vboxPointsButtons.AddChild(ServerSyncButton);
            vboxPointsButtons.AddChild(UnlockButton);

            vboxPoints.AddChild(_pointLabel);
            vboxPoints.AddChild(_pointsPerSecondLabel);
            vboxPoints.AddChild(vboxPointsButtons);

            _technologyIcon = new TextureRect()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
            };
            _technologyName = new Label();
            _technologyDescription = new Label();
            _technologyRequirements = new Label();

            vboxTechInfo.AddChild(_technologyName);
            vboxTechInfo.AddChild(_technologyDescription);
            vboxTechInfo.AddChild(_technologyRequirements);

            hboxSelected.AddChild(_technologyIcon);
            hboxSelected.AddChild(vboxTechInfo);
            hboxSelected.AddChild(vboxPoints);

            hboxTechnologies.AddChild(_unlockedTechnologies);
            hboxTechnologies.AddChild(_unlockableTechnologies);
            hboxTechnologies.AddChild(_futureTechnologies);

            vbox.AddChild(hboxTechnologies);
            vbox.AddChild(hboxSelected);

            Contents.AddChild(vbox);

            UnlockButton.OnPressed += (args) =>
            {
                CleanSelectedTechnology();
            };

            Populate();
        }

        /// <summary>
        ///     Cleans the selected technology controls to blank.
        /// </summary>
        private void CleanSelectedTechnology()
        {
            UnlockButton.Disabled = true;
            _technologyIcon.Texture = Texture.Transparent;
            _technologyName.Text = "";
            _technologyDescription.Text = "";
            _technologyRequirements.Text = "";
        }

        /// <summary>
        ///     Called when an unlocked technology is selected.
        /// </summary>
        private void UnlockedTechnologySelected(ItemList.ItemListSelectedEventArgs obj)
        {
            TechnologySelected = _unlockedTechnologyPrototypes[obj.ItemIndex];

            UnlockButton.Disabled = true;

            PopulateSelectedTechnology();
        }

        /// <summary>
        ///     Called when an unlockable technology is selected.
        /// </summary>
        private void UnlockableTechnologySelected(ItemList.ItemListSelectedEventArgs obj)
        {
            TechnologySelected = _unlockableTechnologyPrototypes[obj.ItemIndex];

            UnlockButton.Disabled = Owner.Points < TechnologySelected.RequiredPoints;

            PopulateSelectedTechnology();
        }

        /// <summary>
        ///     Called when a future technology is selected
        /// </summary>
        private void FutureTechnologySelected(ItemList.ItemListSelectedEventArgs obj)
        {
            TechnologySelected = _futureTechnologyPrototypes[obj.ItemIndex];

            UnlockButton.Disabled = true;

            PopulateSelectedTechnology();
        }

        /// <summary>
        ///     Populate all technologies in the ItemLists.
        /// </summary>
        public void PopulateItemLists()
        {
            _unlockedTechnologies.Clear();
            _unlockableTechnologies.Clear();
            _futureTechnologies.Clear();

            _unlockedTechnologyPrototypes.Clear();
            _unlockableTechnologyPrototypes.Clear();
            _futureTechnologyPrototypes.Clear();

            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();

            // For now, we retrieve all technologies. In the future, this should be changed.
            foreach (var tech in prototypeMan.EnumeratePrototypes<TechnologyPrototype>())
            {
                if (Owner.IsTechnologyUnlocked(tech))
                {
                    _unlockedTechnologies.AddItem(tech.Name, tech.Icon.Frame0());
                    _unlockedTechnologyPrototypes.Add(tech);
                }
                else if (Owner.CanUnlockTechnology(tech))
                {
                    _unlockableTechnologies.AddItem(tech.Name, tech.Icon.Frame0());
                    _unlockableTechnologyPrototypes.Add(tech);
                }
                else
                {
                    _futureTechnologies.AddItem(tech.Name, tech.Icon.Frame0());
                    _futureTechnologyPrototypes.Add(tech);
                }
            }
        }

        /// <summary>
        ///     Fills the selected technology controls with details.
        /// </summary>
        public void PopulateSelectedTechnology()
        {
            if (TechnologySelected == null)
            {
                _technologyName.Text = "";
                _technologyDescription.Text = "";
                _technologyRequirements.Text = "";
                return;
            }

            _technologyIcon.Texture = TechnologySelected.Icon.Frame0();
            _technologyName.Text = TechnologySelected.Name;
            _technologyDescription.Text = TechnologySelected.Description+$"\n{TechnologySelected.RequiredPoints} " + Loc.GetString("research points");
            _technologyRequirements.Text = Loc.GetString("No technology requirements.");

            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();

            for (var i = 0; i < TechnologySelected.RequiredTechnologies.Count; i++)
            {
                var requiredId = TechnologySelected.RequiredTechnologies[i];
                if (!prototypeMan.TryIndex(requiredId, out TechnologyPrototype prototype)) continue;
                if (i == 0)
                    _technologyRequirements.Text = Loc.GetString("Requires") + $": {prototype.Name}";
                else
                    _technologyRequirements.Text += $", {prototype.Name}";
            }
        }

        /// <summary>
        ///     Updates the research point labels.
        /// </summary>
        public void PopulatePoints()
        {
            _pointLabel.Text = Loc.GetString("Research Points") + $": {Owner.Points}";
            _pointsPerSecondLabel.Text = Loc.GetString("Points per second") + $": {Owner.PointsPerSecond}";
        }

        /// <summary>
        ///     Updates the whole user interface.
        /// </summary>
        public void Populate()
        {
            PopulatePoints();
            PopulateSelectedTechnology();
            PopulateItemLists();
        }
    }
}
