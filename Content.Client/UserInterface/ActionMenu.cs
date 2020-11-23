using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// Action selection menu, allows filtering and searching over all possible
    /// actions and populating those actions into the hotbar.
    /// </summary>
    public class ActionMenu : SS14Window
    {
        private static readonly Regex NonAlphanumeric = new Regex(@"\W", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly int MinSearchLength = 3;
        private static ActionPrototype[] EmptyActionList = new ActionPrototype[0];


        // parallel list of actions currently selectable in itemList
        private ActionPrototype[] _actionList;

        private readonly ActionManager _actionManager;
        private readonly ClientActionsComponent _actionsComponent;
        private readonly VBoxContainer _mainVBox;
        private readonly LineEdit _searchBar;
        private readonly MultiselectOptionButton<string> _filterButton;
        private readonly Label _filterLabel;
        private readonly Button _clearButton;
        private readonly GridContainer _resultsGrid;


        private event Action<ActionMenuItemSelectedEventArgs> _onItemSelected;

        /// <param name="actionsComponent">component to use to lookup action statuses</param>
        /// <param name="onItemSelected">invoked when an action item
        /// in the list is clicked</param>
        public ActionMenu(ClientActionsComponent actionsComponent, Action<ActionMenuItemSelectedEventArgs> onItemSelected)
        {
            _actionsComponent = actionsComponent;
            _onItemSelected = onItemSelected;
            _actionManager = IoCManager.Resolve<ActionManager>();
            Title = Loc.GetString("Actions");
            CustomMinimumSize = (300, 300);

            Contents.AddChild(_mainVBox = new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (_searchBar = new LineEdit
                            {
                                StyleClasses = { StyleNano.StyleClassActionSearchBox },
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                PlaceHolder = Loc.GetString("Search")
                            }),
                            (_filterButton = new MultiselectOptionButton<string>()
                            {
                                Label = Loc.GetString("Filter")
                            }),
                        }
                    },
                    (_clearButton = new Button
                    {
                        Text = Loc.GetString("Clear"),
                    }),
                    (_filterLabel = new Label()),
                    new ScrollContainer
                    {
                        //TODO: needed? CustomMinimumSize = new Vector2(200.0f, 0.0f),
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            (_resultsGrid = new GridContainer
                            {
                                MaxWidth = 300
                            })
                        }
                    }
                }
            });

            _clearButton.OnPressed += OnClearButtonPressed;
            _searchBar.OnTextChanged += OnSearchTextChanged;
            _filterButton.OnItemSelected += OnFilterItemSelected;

            // populate filters from search tags
            var searchTags = _actionManager.EnumerateActions()
                .SelectMany(a => a.SearchTags)
                .Distinct()
                .OrderBy(tag => tag);
            foreach (var tag in searchTags)
            {
                _filterButton.AddItem( CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tag), tag);
            }

            UpdateFilterLabel();
        }

        private void OnFilterItemSelected(MultiselectOptionButton<string>.ItemPressedEventArgs args)
        {
            UpdateFilterLabel();
            SearchAndDisplay();
        }

        protected override void Resized()
        {
            base.Resized();
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // currently no good way to let the grid know what size it has to "work with", so must manually resize
            _resultsGrid.MaxWidth = Width;
        }

        private void OnItemPressed(BaseButton.ButtonEventArgs args)
        {
            _onItemSelected?.Invoke(new ActionMenuItemSelectedEventArgs((args.Button as ActionMenuItem).Action));
        }

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _searchBar.Clear();
            _filterButton.DeselectAll();
            UpdateFilterLabel();
            SearchAndDisplay();
        }

        private void OnSearchTextChanged(LineEdit.LineEditEventArgs obj)
        {
            SearchAndDisplay();
        }

        private void SearchAndDisplay()
        {
            var search = Standardize(_searchBar.Text);
            // only display nothing if there are no filters selected and text is not long enough.
            // otherwise we will search if even one filter is applied, regardless of length of search string
            if (_filterButton.SelectedKeys.Count == 0 &&
                (string.IsNullOrWhiteSpace(search) || search.Length < MinSearchLength))
            {
                ClearList();
                return;
            }

            var matchingActions = _actionManager.EnumerateActions()
                .Where(a => MatchesSearchCriteria(a, search, _filterButton.SelectedKeys));

            PopulateActions(matchingActions);
        }

        private void UpdateFilterLabel()
        {
            if (_filterButton.SelectedKeys.Count == 0)
            {
                _filterLabel.Visible = false;
            }
            else
            {
                _filterLabel.Visible = true;
                _filterLabel.Text = Loc.GetString("Filters") + ": " +
                                    string.Join(", ", _filterButton.SelectedLabels);
            }
        }

        private bool MatchesSearchCriteria(ActionPrototype action, string search,
            IReadOnlyList<string> tags)
        {
            // check tag match first - each action must contain all tags currently selected.
            // if no tags selected, don't check tags
            if (tags.Count > 0 && tags.Any(tag => !action.SearchTags.Contains(tag)))
            {
                return false;
            }

            if (Standardize(action.ActionType.ToString()).Contains(search))
            {
                return true;
            }

            // allows matching by typing spaces between the enum case changes, like "xeno spit" if the
            // actiontype is "XenoSpit"
            if (Standardize(action.ActionType.ToString(), true).Contains(search))
            {
                return true;
            }

            if (Standardize(action.Name.ToString()).Contains(search))
            {
                return true;
            }

            return false;

        }


        private static string Standardize(string rawText, bool splitOnCaseChange = false)
        {
            rawText ??= "";

            // treat non-alphanumeric characters as whitespace
            rawText = NonAlphanumeric.Replace(rawText, " ");

            // trim spaces and reduce internal whitespaces to 1 max
            rawText = Whitespace.Replace(rawText, " ").Trim();
            if (splitOnCaseChange)
            {
                // insert a space when case switches from lower to upper
                rawText = AddSpaces(rawText, true);
            }

            return rawText.ToLowerInvariant().Trim();
        }

        // taken from https://stackoverflow.com/a/272929 (CC BY-SA 3.0)
        private static string AddSpaces(string text, bool preserveAcronyms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        private void PopulateActions(IEnumerable<ActionPrototype> actions)
        {
            ClearList();

            _actionList = actions.ToArray();
            foreach (var action in _actionList.OrderBy(act => act.Name.ToString()))
            {
                var actionItem = new ActionMenuItem(action);
                _resultsGrid.Children.Add(actionItem);
                actionItem.SetActionState(_actionsComponent.IsGranted(action.ActionType));

                actionItem.OnPressed += OnItemPressed;
            }
        }

        private void ClearList()
        {
            // TODO: Not sure if this unsub is needed if children are all being cleared
            foreach (var actionItem in _resultsGrid.Children)
            {
                (actionItem as ActionMenuItem).OnPressed -= OnItemPressed;
            }
            _resultsGrid.Children.Clear();
            _actionList = EmptyActionList;
        }

        /// <summary>
        /// Should be invoked when action states change, ensures
        /// currently displayed actions are properly showing their revoked / granted status
        /// </summary>
        public void UpdateActionStates()
        {
            foreach (var actionItem in _resultsGrid.Children)
            {
                var actionMenuItem = (actionItem as ActionMenuItem);
                actionMenuItem.SetActionState(_actionsComponent.IsGranted(actionMenuItem.Action.ActionType));
            }
        }
    }

    public class ActionMenuItemSelectedEventArgs : EventArgs
    {
        public readonly ActionPrototype Action;

        public ActionMenuItemSelectedEventArgs(ActionPrototype action)
        {
            Action = action;
        }
    }
}
