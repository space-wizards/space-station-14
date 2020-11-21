using System;
using System.Collections.Generic;
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
        private readonly  VBoxContainer _mainVBox;
        private readonly LineEdit _searchBar;
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

                            (_clearButton = new Button
                            {
                                Disabled = true,
                                Text = Loc.GetString("Clear"),
                            })
                        }
                    },
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
            ClearList();
        }

        private void OnSearchTextChanged(LineEdit.LineEditEventArgs obj)
        {
            var search = Standardize(obj.Text);
            if (string.IsNullOrWhiteSpace(search) || search.Length < MinSearchLength)
            {
                ClearList();
                return;
            }

            // search on names
            var matchingActions = _actionManager.EnumerateActions()
                .Where(a => MatchesSearch(a, search));

            PopulateActions(matchingActions);
        }

        private bool MatchesSearch(ActionPrototype action, string search)
        {
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

            // TODO: Matching on tags

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

        protected override void Opened()
        {
            base.Opened();
            PopulateActions(_actionManager.EnumerateActions());
        }

        private void PopulateActions(IEnumerable<ActionPrototype> actions)
        {
            ClearList();

            _actionList = actions.ToArray();
            foreach (var action in _actionList)
            {
                var actionItem = new ActionMenuItem(action);
                _resultsGrid.Children.Add(actionItem);
                actionItem.SetActionState(_actionsComponent.IsGranted(action.ActionType));

                actionItem.OnPressed += OnItemPressed;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ClearList();
            // TODO: this probably isn't needed since these are children of this control
            _clearButton.OnPressed -= OnClearButtonPressed;
            _searchBar.OnTextChanged -= OnSearchTextChanged;
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
