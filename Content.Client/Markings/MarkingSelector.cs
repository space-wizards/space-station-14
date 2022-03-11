using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Stylesheets;
using Content.Shared.Markings;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Markings
{
    public sealed class MarkingPicker : Control
    {
        [Dependency] private readonly MarkingManager _markingManager = default!;

        // temporarily, as a treat
        // maybe use this information to create
        // a 'realistic' enum of skin colors?
        // public Action<Color>? OnBodyColorChange;
        public Action<List<Marking>>? OnMarkingAdded;
        public Action<List<Marking>>? OnMarkingRemoved;
        public Action<List<Marking>>? OnMarkingColorChange;
        public Action<List<Marking>>? OnMarkingRankChange;

        private readonly ItemList _unusedMarkings;
        private readonly ItemList _usedMarkings;

        /*
        private readonly Control _bodyColorContainer;
        private readonly ColorSlider _bodyColorSliderR;
        private readonly ColorSlider _bodyColorSliderG;
        private readonly ColorSlider _bodyColorSliderB;
        */


        private readonly Control _colorContainer;

        private readonly OptionButton _markingCategoryButton;

        private readonly Button _addMarkingButton;
        private readonly Button _upRankMarkingButton;
        private readonly Button _downRankMarkingButton;
        private readonly Button _removeMarkingButton;

        private List<Color> _currentMarkingColors = new();

        private ItemList.Item? _selectedMarking;
        private ItemList.Item? _selectedUnusedMarking;
        private MarkingCategories _selectedMarkingCategory = MarkingCategories.Chest;
        private List<Marking> _usedMarkingList = new();

        private List<MarkingCategories> _markingCategories = Enum.GetValues<MarkingCategories>().ToList();

        private string _currentSpecies = "Human"; // mmmmm

        public void SetData(List<Marking> newMarkings, string species)
        {
            _usedMarkingList = newMarkings;
            _usedMarkings.Clear();
            _selectedMarking = null;
            _selectedUnusedMarking = null;
            _currentSpecies = species;

            Logger.DebugS("MarkingSelector", $"New marking set: {_usedMarkingList}");
            // foreach (var m in _usedMarkingList)
            for (int i = 0; i < _usedMarkingList.Count; i++)
            {
                Marking marking = _usedMarkingList[i];
                if (_markingManager.IsValidMarking(marking, out MarkingPrototype? newMarking))
                {
                    // TODO: Composite sprite preview, somehow.
                    var _item = _usedMarkings.AddItem($"{newMarking.ID} ({newMarking.MarkingCategory})", newMarking.Sprites[0].Frame0());
                    _item.Metadata = newMarking;
                    _item.IconModulate = marking.MarkingColors[0];
                    if (marking.MarkingColors.Count != _usedMarkingList[i].MarkingColors.Count)
                    {
                        _usedMarkingList[i] = new Marking(marking.MarkingId, marking.MarkingColors);
                    }
                }

                foreach (var unusedMarking in _unusedMarkings)
                {
                    if (unusedMarking.Metadata == newMarking)
                    {
                        _unusedMarkings.Remove(unusedMarking);
                        break;
                    }
                }
            }

            /*
            _bodyColorSliderR.ColorValue = newBodyColor.RByte;
            _bodyColorSliderG.ColorValue = newBodyColor.GByte;
            _bodyColorSliderB.ColorValue = newBodyColor.BByte;
            */
        }

        public MarkingPicker()
        {
            IoCManager.InjectDependencies(this);

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            AddChild(vBox);

            /* No longer needed due to species being in main.
            var speciesButtonContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };
            _speciesButton = new OptionButton
            {
                HorizontalExpand = true
            };
            _availableSpecies = _speciesManager.AvailableSpecies();
            for (int i = 0; i < _availableSpecies.Count; i++)
                _speciesButton.AddItem(_availableSpecies[i], i);

            _speciesButton.OnItemSelected += args =>
            {
                _speciesButton.SelectId(args.Id);
                SetSpecies(_availableSpecies[args.Id]);
            };
            speciesButtonContainer.AddChild(new Label { Text = "Species sprite base:" });
            speciesButtonContainer.AddChild(_speciesButton);
            vBox.AddChild(speciesButtonContainer);
            */


            /*
            // remember to remove this later
            _bodyColorContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
            };

            vBox.AddChild(_bodyColorContainer);
            _bodyColorContainer.AddChild(new Label { Text = "Current body color:" });
            _bodyColorContainer.AddChild(_bodyColorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed));
            _bodyColorContainer.AddChild(_bodyColorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen));
            _bodyColorContainer.AddChild(_bodyColorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue));

            Action bodyColorChanged = BodyColorChanged;
            _bodyColorSliderR.OnValueChanged += bodyColorChanged;
            _bodyColorSliderG.OnValueChanged += bodyColorChanged;
            _bodyColorSliderB.OnValueChanged += bodyColorChanged;
            */


            var markingListContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };
            var unusedMarkingsContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            unusedMarkingsContainer.AddChild(new Label { Text = "Unused markings:" });
            _markingCategoryButton = new OptionButton
            {
                HorizontalExpand = true
            };
            for (int i = 0; i < _markingCategories.Count; i++)
            {
                _markingCategoryButton.AddItem(_markingCategories[i].ToString(), i);
            }
            _markingCategoryButton.SelectId(_markingCategories.IndexOf(MarkingCategories.Chest));
            _markingCategoryButton.OnItemSelected +=  OnCategoryChange;
            unusedMarkingsContainer.AddChild(_markingCategoryButton);
            _unusedMarkings = new ItemList
            {
                VerticalExpand = true,
                MinSize = (300, 250)
            };
            _unusedMarkings.OnItemSelected += item =>
               _selectedUnusedMarking = _unusedMarkings[item.ItemIndex];

            _addMarkingButton = new Button
            {
                Text = "Add Marking"
            };
            _addMarkingButton.OnPressed += args =>
                MarkingAdd();
            unusedMarkingsContainer.AddChild(_unusedMarkings);
            unusedMarkingsContainer.AddChild(_addMarkingButton);
            markingListContainer.AddChild(unusedMarkingsContainer);

            var usedMarkingsContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            usedMarkingsContainer.AddChild(new Label { Text = "Current markings:" });
            _usedMarkings = new ItemList
            {
                VerticalExpand = true,
                MinSize = (300, 250)
            };
            _usedMarkings.OnItemSelected += OnUsedMarkingSelected;

            var buttonRankingContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };
            _upRankMarkingButton = new Button
            {
                Text = "Up",
                HorizontalExpand = true,
            };
            _downRankMarkingButton = new Button
            {
                Text = "Down",
                HorizontalExpand = true,
            };
            buttonRankingContainer.AddChild(_upRankMarkingButton);
            buttonRankingContainer.AddChild(_downRankMarkingButton);

            _removeMarkingButton = new Button
            {
                Text = "Remove Marking"
            };
            _removeMarkingButton.OnPressed += args =>
                MarkingRemove();

            usedMarkingsContainer.AddChild(_usedMarkings);
            usedMarkingsContainer.AddChild(buttonRankingContainer);
            usedMarkingsContainer.AddChild(_removeMarkingButton);
            markingListContainer.AddChild(usedMarkingsContainer);

            vBox.AddChild(markingListContainer);

            _colorContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Visible = false
            };
            vBox.AddChild(_colorContainer);
        }

        public void Populate()
        {
            _unusedMarkings.Clear();
            var markings = _markingManager.CategorizedMarkings();
            foreach (var marking in markings[_selectedMarkingCategory])
            {
                if (_usedMarkingList.Contains(marking.AsMarking())) continue;
                if (!marking.SpeciesRestrictions.Contains(_currentSpecies) && !marking.Unrestricted) continue;
                Logger.DebugS("MarkingSelector", $"Adding marking {marking.ID}");
                var item = _unusedMarkings.AddItem($"{marking.ID}", marking.Sprites[0].Frame0());
                item.Metadata = marking;
            }
        }

        // repopulate in case markings are restricted,
        // and also filter out any markings that are now invalid
        public void SetSpecies(string species)
        {
            _currentSpecies = species;
            var markingCount = _usedMarkings.Count;
            for (int i = 0; i < _usedMarkings.Count; i++)
            {
                var markingPrototype = (MarkingPrototype) _usedMarkings[i].Metadata!;
                if (!markingPrototype.SpeciesRestrictions.Contains(species))
                {
                    _usedMarkings.RemoveAt(i);
                    _usedMarkingList.RemoveAt(i);
                }

            }

            if (markingCount != _usedMarkings.Count)
            {
                OnMarkingRemoved?.Invoke(_usedMarkingList);
            }

            Populate();
        }

        private void OnCategoryChange(OptionButton.ItemSelectedEventArgs category)
        {
            _markingCategoryButton.SelectId(category.Id);
            _selectedMarkingCategory = _markingCategories[category.Id];
            Populate();
        }

        private void OnUsedMarkingSelected(ItemList.ItemListSelectedEventArgs item)
        {
            _selectedMarking = _usedMarkings[item.ItemIndex];
            var prototype = (MarkingPrototype) _selectedMarking.Metadata!;
            _currentMarkingColors.Clear();
            _colorContainer.RemoveAllChildren();
            List<List<ColorSlider>> colorSliders = new();
            for (int i = 0; i < prototype.Sprites.Count; i++)
            {
                var colorContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                };

                _colorContainer.AddChild(colorContainer);

                List<ColorSlider> sliders = new();
                ColorSlider colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed);
                ColorSlider colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen);
                ColorSlider colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue);

                colorContainer.AddChild(new Label { Text = $"{prototype.MarkingPartNames[i]} color:" });
                colorContainer.AddChild(colorSliderR);
                colorContainer.AddChild(colorSliderG);
                colorContainer.AddChild(colorSliderB);

                var currentColor = new Color(
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].RByte,
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].GByte,
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].BByte
                );
                _currentMarkingColors.Add(currentColor);
                int colorIndex = _currentMarkingColors.IndexOf(currentColor);

                colorSliderR.ColorValue = currentColor.RByte;
                colorSliderG.ColorValue = currentColor.GByte;
                colorSliderB.ColorValue = currentColor.BByte;

                Action colorChanged = delegate()
                {
                    _currentMarkingColors[colorIndex] = new Color(
                        colorSliderR.ColorValue,
                        colorSliderG.ColorValue,
                        colorSliderB.ColorValue
                    );

                    ColorChanged(colorIndex);
                };
                colorSliderR.OnValueChanged += colorChanged;
                colorSliderG.OnValueChanged += colorChanged;
                colorSliderB.OnValueChanged += colorChanged;
            }

            _colorContainer.Visible = true;
        }

        /*
        private void BodyColorChanged()
        {
            var newColor = new Color(
                _bodyColorSliderR.ColorValue,
                _bodyColorSliderG.ColorValue,
                _bodyColorSliderB.ColorValue
            );

            OnBodyColorChange?.Invoke(newColor);
        }
        */


        private void ColorChanged(int colorIndex)
        {
            if (_selectedMarking is null) return;
            var markingPrototype = (MarkingPrototype) _selectedMarking.Metadata!;
            int markingIndex = _usedMarkingList.FindIndex(m => m.MarkingId == markingPrototype.ID);

            if (markingIndex < 0) return; // ???

            /*
            var newColor = new Color(
                _selectedMarkingColor[0].ColorValue,
                _selectedMarkingColor[1].ColorValue,
                _selectedMarkingColor[2].ColorValue
            );
            */

            _selectedMarking.IconModulate = _currentMarkingColors[colorIndex];
            _usedMarkingList[markingIndex].SetColor(colorIndex, _currentMarkingColors[colorIndex]);
            OnMarkingColorChange?.Invoke(_usedMarkingList);
        }

        private void MarkingAdd()
        {
            if (_usedMarkingList is null || _selectedUnusedMarking is null) return;

            MarkingPrototype marking = (MarkingPrototype) _selectedUnusedMarking.Metadata!;
            Logger.DebugS("MarkingSelector", $"Adding marking {marking.ID} to character");
            _usedMarkingList.Add(marking.AsMarking());
            Logger.DebugS("MarkingSelector", $"{_usedMarkingList}");

            _unusedMarkings.Remove(_selectedUnusedMarking);
            var item = _usedMarkings.AddItem($"{marking.ID} ({marking.MarkingCategory})", marking.Sprites[0].Frame0());
            item.Metadata = marking;

            _selectedUnusedMarking = null;
            OnMarkingAdded?.Invoke(_usedMarkingList);
        }

        private void MarkingRemove()
        {
            if (_usedMarkingList is null || _selectedMarking is null) return;

            MarkingPrototype marking = (MarkingPrototype) _selectedMarking.Metadata!;
            _usedMarkingList.Remove(marking.AsMarking());
            _usedMarkings.Remove(_selectedMarking);

            if (marking.MarkingCategory == _selectedMarkingCategory)
            {
                var item = _unusedMarkings.AddItem($"{marking.ID}", marking.Sprites[0].Frame0());
                item.Metadata = marking;
            }
            _selectedMarking = null;
            _colorContainer.Visible = false;
            OnMarkingRemoved?.Invoke(_usedMarkingList);
        }
    }
}
