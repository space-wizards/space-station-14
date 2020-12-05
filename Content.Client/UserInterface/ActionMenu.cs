using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// Action selection menu, allows filtering and searching over all possible
    /// actions and populating those actions into the hotbar.
    /// </summary>
    public class ActionMenu : SS14Window
    {
        private static readonly string ItemTag = "item";
        private static readonly string NotItemTag = "not item";
        private static readonly string InstantActionTag = "instant";
        private static readonly string ToggleActionTag = "toggle";
        private static readonly string TargetActionTag = "target";
        private static readonly Regex NonAlphanumeric = new Regex(@"\W", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly int MinSearchLength = 3;
        private static BaseActionPrototype[] EmptyActionList = new ActionPrototype[0];



        // parallel list of actions currently selectable in itemList
        private BaseActionPrototype[] _actionList;

        private readonly ActionManager _actionManager;
        private readonly ClientActionsComponent _actionsComponent;
        private readonly VBoxContainer _mainVBox;
        private readonly LineEdit _searchBar;
        private readonly MultiselectOptionButton<string> _filterButton;
        private readonly Label _filterLabel;
        private readonly Button _clearButton;
        private readonly GridContainer _resultsGrid;
        private readonly EventHandler _onShowTooltip;
        private readonly EventHandler _onHideTooltip;
        private readonly TextureRect _dragShadow;
        private readonly DragDropHelper<ActionMenuItem> _dragDropHelper;


        private readonly Action<ActionMenuItemSelectedEventArgs> _onItemSelected;
        private readonly Action<ActionMenuItemDragDropEventArgs> _onItemDragDrop;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each ActionMenuItem</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each ActionMenuItem</param>
        /// <param name="actionsComponent">component to use to lookup action statuses</param>
        /// <param name="onItemSelected">invoked when an action item
        /// in the list is clicked</param>
        /// <param name="onItemDragDrop">invoked when an action item
        /// in the list is dragged and dropped onto a hotbar slot</param>
        public ActionMenu(EventHandler onShowTooltip, EventHandler onHideTooltip, ClientActionsComponent actionsComponent,
            Action<ActionMenuItemSelectedEventArgs> onItemSelected,
            Action<ActionMenuItemDragDropEventArgs> onItemDragDrop)
        {
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _actionsComponent = actionsComponent;
            _onItemSelected = onItemSelected;
            _onItemDragDrop = onItemDragDrop;
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
            var filterTags = _actionManager.EnumerateActions()
                .SelectMany(a => a.Filters).ToList();

            // special one to filter to only include item actions
            filterTags.Add(ItemTag);
            filterTags.Add(NotItemTag);
            filterTags.Add(InstantActionTag);
            filterTags.Add(ToggleActionTag);
            filterTags.Add(TargetActionTag);

            foreach (var tag in filterTags.Distinct().OrderBy(tag => tag))
            {
                _filterButton.AddItem( CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tag), tag);
            }

            UpdateFilterLabel();

            _dragShadow = new TextureRect
            {
                CustomMinimumSize = (64, 64),
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            UserInterfaceManager.PopupRoot.AddChild(_dragShadow);
            LayoutContainer.SetSize(_dragShadow, (64, 64));

            _dragDropHelper = new DragDropHelper<ActionMenuItem>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _clearButton.OnPressed -= OnClearButtonPressed;
            _searchBar.OnTextChanged -= OnSearchTextChanged;
            _filterButton.OnItemSelected -= OnFilterItemSelected;

            foreach (var actionMenuControl in _resultsGrid.Children)
            {
                var actionMenuItem = (actionMenuControl as ActionMenuItem);
                actionMenuItem.OnButtonDown -= OnItemButtonDown;
                actionMenuItem.OnButtonUp -= OnItemButtonUp;
                actionMenuItem.OnPressed -= OnItemPressed;
                actionMenuItem.OnShowTooltip -= _onShowTooltip;
                actionMenuItem.OnHideTooltip -= _onHideTooltip;
            }
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

        private bool OnBeginActionDrag()
        {
            _dragShadow.Texture = _dragDropHelper.Dragged.Action.Icon.Frame0();
            // don't make visible until frameupdate, otherwise it'll flicker
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // keep dragged entity centered under mouse
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            // we don't set this visible until frameupdate, otherwise it flickers
            _dragShadow.Visible = true;
            return true;
        }

        private void OnEndActionDrag()
        {
            _dragShadow.Visible = false;
        }

        private void OnItemButtonDown(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick) return;
            _dragDropHelper.MouseDown(args.Button as ActionMenuItem);
        }

        private void OnItemButtonUp(BaseButton.ButtonEventArgs args)
        {
            // note the buttonup only fires on the control that was originally
            // pressed to initiate the drag, NOT the one we are currently hovering
            if (args.Event.Function != EngineKeyFunctions.UIClick) return;

            if (UserInterfaceManager.CurrentlyHovered != null &&
                UserInterfaceManager.CurrentlyHovered is ActionSlot targetSlot)
            {
                if (!_dragDropHelper.IsDragging || _dragDropHelper.Dragged?.Action == null)
                {
                    _dragDropHelper.EndDrag();
                    return;
                }

                // drag and drop
                _onItemDragDrop?.Invoke(new ActionMenuItemDragDropEventArgs(_dragDropHelper.Dragged, targetSlot));
            }

            _dragDropHelper.EndDrag();
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

        private bool MatchesSearchCriteria(BaseActionPrototype action, string standardizedSearch,
            IReadOnlyList<string> selectedFilterTags)
        {
            // check filter tag match first - each action must contain all filter tags currently selected.
            // if no tags selected, don't check tags
            if (selectedFilterTags.Count > 0 && selectedFilterTags.Any(filterTag => !ActionMatchesFilterTag(action, filterTag)))
            {
                return false;
            }

            // check search tag match against the search query
            if (action.Keywords.Any(standardizedSearch.Contains))
            {
                return true;
            }

            if (Standardize(ActionTypeString(action)).Contains(standardizedSearch))
            {
                return true;
            }

            // allows matching by typing spaces between the enum case changes, like "xeno spit" if the
            // actiontype is "XenoSpit"
            if (Standardize(ActionTypeString(action), true).Contains(standardizedSearch))
            {
                return true;
            }

            if (Standardize(action.Name.ToString()).Contains(standardizedSearch))
            {
                return true;
            }

            return false;

        }

        private string ActionTypeString(BaseActionPrototype baseActionPrototype)
        {
            if (baseActionPrototype is ActionPrototype actionPrototype)
            {
                return actionPrototype.ActionType.ToString();
            }
            if (baseActionPrototype is ItemActionPrototype itemActionPrototype)
            {
                return itemActionPrototype.ActionType.ToString();
            }
            throw new InvalidOperationException();
        }

        private static bool ActionMatchesFilterTag(BaseActionPrototype action, string tag)
        {
            if (tag == ItemTag)
            {
                return action is ItemActionPrototype;
            }

            if (tag == NotItemTag)
            {
                return action is ActionPrototype;
            }
            return action.Filters.Contains(tag);
        }


        /// <summary>
        /// Standardized form is all lowercase, no non-alphanumeric characters (converted to whitespace),
        /// trimmed, 1 space max per whitespace gap,
        /// and optional spaces between case change
        /// </summary>
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

        private void PopulateActions(IEnumerable<BaseActionPrototype> actions)
        {
            ClearList();

            _actionList = actions.ToArray();
            foreach (var action in _actionList.OrderBy(act => act.Name.ToString()))
            {
                var actionItem = new ActionMenuItem(action);
                _resultsGrid.Children.Add(actionItem);
                actionItem.SetActionState(IsGranted(action));

                actionItem.OnButtonDown += OnItemButtonDown;
                actionItem.OnButtonUp += OnItemButtonUp;
                actionItem.OnPressed += OnItemPressed;
                actionItem.OnShowTooltip += _onShowTooltip;
                actionItem.OnHideTooltip += _onHideTooltip;
            }
        }

        private bool IsGranted(BaseActionPrototype baseActionPrototype)
        {
            if (baseActionPrototype is ActionPrototype actionPrototype)
            {
                return _actionsComponent.IsGranted(actionPrototype.ActionType);
            }

            if (baseActionPrototype is ItemActionPrototype itemActionPrototype)
            {
                return _actionsComponent.IsGranted(itemActionPrototype.ActionType);
            }

            return false;
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
        public void UpdateUI()
        {
            foreach (var actionItem in _resultsGrid.Children)
            {
                var actionMenuItem = (actionItem as ActionMenuItem);
                actionMenuItem.SetActionState(IsGranted(actionMenuItem.Action));
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.Update(args);
            _dragDropHelper.Update(args.DeltaSeconds);
        }
    }

    public class ActionMenuItemSelectedEventArgs : EventArgs
    {
        public readonly BaseActionPrototype Action;

        public ActionMenuItemSelectedEventArgs(BaseActionPrototype action)
        {
            Action = action;
        }
    }

    /// <summary>
    /// Args for dragging and dropping an action menu item onto a hotbar slot.
    /// </summary>
    public class ActionMenuItemDragDropEventArgs : EventArgs
    {
        public readonly ActionMenuItem ActionMenuItem;
        public readonly ActionSlot ToSlot;

        public ActionMenuItemDragDropEventArgs(ActionMenuItem actionMenuItem, ActionSlot toSlot)
        {
            ActionMenuItem = actionMenuItem;
            ToSlot = toSlot;
        }
    }
}
