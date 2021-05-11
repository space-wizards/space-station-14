using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.GameObjects.Components.Mobs.Actions;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Actions;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// Action selection menu, allows filtering and searching over all possible
    /// actions and populating those actions into the hotbar.
    /// </summary>
    public class ActionMenu : SS14Window
    {
        private const string ItemTag = "item";
        private const string NotItemTag = "not item";
        private const string InstantActionTag = "instant";
        private const string ToggleActionTag = "toggle";
        private const string TargetActionTag = "target";
        private const string AllActionsTag = "all";
        private const string GrantedActionsTag = "granted";
        private const int MinSearchLength = 3;
        private static readonly Regex NonAlphanumeric = new Regex(@"\W", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly BaseActionPrototype[] EmptyActionList = Array.Empty<BaseActionPrototype>();

        /// <summary>
        /// Is an action currently being dragged from this window?
        /// </summary>
        public bool IsDragging => _dragDropHelper.IsDragging;

        // parallel list of actions currently selectable in itemList
        private BaseActionPrototype[] _actionList = new BaseActionPrototype[0];

        private readonly ActionManager _actionManager;
        private readonly ClientActionsComponent _actionsComponent;
        private readonly ActionsUI _actionsUI;
        private readonly LineEdit _searchBar;
        private readonly MultiselectOptionButton<string> _filterButton;
        private readonly Label _filterLabel;
        private readonly Button _clearButton;
        private readonly GridContainer _resultsGrid;
        private readonly TextureRect _dragShadow;
        private readonly IGameHud _gameHud;
        private readonly DragDropHelper<ActionMenuItem> _dragDropHelper;


        public ActionMenu(ClientActionsComponent actionsComponent, ActionsUI actionsUI)
        {
            _actionsComponent = actionsComponent;
            _actionsUI = actionsUI;
            _actionManager = IoCManager.Resolve<ActionManager>();
            _gameHud = IoCManager.Resolve<IGameHud>();

            Title = Loc.GetString("Actions");
            MinSize = (300, 300);

            Contents.AddChild(new VBoxContainer
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
                                HorizontalExpand = true,
                                PlaceHolder = Loc.GetString("Search")
                            }),
                            (_filterButton = new MultiselectOptionButton<string>()
                            {
                                Label = Loc.GetString("Filter")
                            })
                        }
                    },
                    (_clearButton = new Button
                    {
                        Text = Loc.GetString("Clear"),
                    }),
                    (_filterLabel = new Label()),
                    new ScrollContainer
                    {
                        //TODO: needed? MinSize = new Vector2(200.0f, 0.0f),
                        VerticalExpand = true,
                        HorizontalExpand = true,
                        Children =
                        {
                            (_resultsGrid = new GridContainer
                            {
                                MaxGridWidth = 300
                            })
                        }
                    }
                }
            });

            // populate filters from search tags
            var filterTags = new List<string>();
            foreach (var action in _actionManager.EnumerateActions())
            {
                filterTags.AddRange(action.Filters);
            }

            // special one to filter to only include item actions
            filterTags.Add(ItemTag);
            filterTags.Add(NotItemTag);
            filterTags.Add(InstantActionTag);
            filterTags.Add(ToggleActionTag);
            filterTags.Add(TargetActionTag);
            filterTags.Add(AllActionsTag);
            filterTags.Add(GrantedActionsTag);

            foreach (var tag in filterTags.Distinct().OrderBy(tag => tag))
            {
                _filterButton.AddItem( CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tag), tag);
            }

            UpdateFilterLabel();

            _dragShadow = new TextureRect
            {
                MinSize = (64, 64),
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false,
                SetSize = (64, 64)
            };
            UserInterfaceManager.PopupRoot.AddChild(_dragShadow);

            _dragDropHelper = new DragDropHelper<ActionMenuItem>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _clearButton.OnPressed += OnClearButtonPressed;
            _searchBar.OnTextChanged += OnSearchTextChanged;
            _filterButton.OnItemSelected += OnFilterItemSelected;
            _gameHud.ActionsButtonDown = true;
            foreach (var actionMenuControl in _resultsGrid.Children)
            {
                var actionMenuItem = (ActionMenuItem) actionMenuControl;
                actionMenuItem.OnButtonDown += OnItemButtonDown;
                actionMenuItem.OnButtonUp += OnItemButtonUp;
                actionMenuItem.OnPressed += OnItemPressed;
            }
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _dragDropHelper.EndDrag();
            _clearButton.OnPressed -= OnClearButtonPressed;
            _searchBar.OnTextChanged -= OnSearchTextChanged;
            _filterButton.OnItemSelected -= OnFilterItemSelected;
            _gameHud.ActionsButtonDown = false;
            foreach (var actionMenuControl in _resultsGrid.Children)
            {
                var actionMenuItem = (ActionMenuItem) actionMenuControl;
                actionMenuItem.OnButtonDown -= OnItemButtonDown;
                actionMenuItem.OnButtonUp -= OnItemButtonUp;
                actionMenuItem.OnPressed -= OnItemPressed;
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
            _resultsGrid.MaxGridWidth = Width;
        }

        private bool OnBeginActionDrag()
        {
            _dragShadow.Texture = _dragDropHelper.Dragged!.Action.Icon.Frame0();
            // don't make visible until frameupdate, otherwise it'll flicker
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled.Position - (32, 32));
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // keep dragged entity centered under mouse
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled.Position - (32, 32));
            // we don't set this visible until frameupdate, otherwise it flickers
            _dragShadow.Visible = true;
            return true;
        }

        private void OnEndActionDrag()
        {
            _dragShadow.Visible = false;
        }

        private void OnItemButtonDown(ButtonEventArgs args)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick ||
                args.Button is not ActionMenuItem action)
            {
                return;
            }

            _dragDropHelper.MouseDown(action);
        }

        private void OnItemButtonUp(ButtonEventArgs args)
        {
            // note the buttonup only fires on the control that was originally
            // pressed to initiate the drag, NOT the one we are currently hovering
            if (args.Event.Function != EngineKeyFunctions.UIClick) return;

            if (UserInterfaceManager.CurrentlyHovered is ActionSlot targetSlot)
            {
                if (!_dragDropHelper.IsDragging || _dragDropHelper.Dragged?.Action == null)
                {
                    _dragDropHelper.EndDrag();
                    return;
                }

                // drag and drop
                switch (_dragDropHelper.Dragged.Action)
                {
                    // assign the dragged action to the target slot
                    case ActionPrototype actionPrototype:
                        _actionsComponent.Assignments.AssignSlot(_actionsUI.SelectedHotbar, targetSlot.SlotIndex, ActionAssignment.For(actionPrototype.ActionType));
                        break;
                    case ItemActionPrototype itemActionPrototype:
                        // the action menu doesn't show us if the action has an associated item,
                        // so when we perform the assignment, we should check if we currently have an unassigned state
                        // for this item and assign it tied to that item if so, otherwise assign it "itemless"

                        // this is not particularly efficient but we don't maintain an index from
                        // item action type to its action states, and this method should be pretty infrequent so it's probably fine
                        var assigned = false;
                        foreach (var (item, itemStates) in _actionsComponent.ItemActionStates())
                        {
                            foreach (var (actionType, _) in itemStates)
                            {
                                if (actionType != itemActionPrototype.ActionType) continue;
                                var assignment = ActionAssignment.For(actionType, item);
                                if (_actionsComponent.Assignments.HasAssignment(assignment)) continue;
                                // no assignment for this state, assign tied to the item
                                assigned = true;
                                _actionsComponent.Assignments.AssignSlot(_actionsUI.SelectedHotbar, targetSlot.SlotIndex, assignment);
                                break;
                            }

                            if (assigned)
                            {
                                break;
                            }
                        }

                        if (!assigned)
                        {
                            _actionsComponent.Assignments.AssignSlot(_actionsUI.SelectedHotbar, targetSlot.SlotIndex, ActionAssignment.For(itemActionPrototype.ActionType));
                        }
                        break;
                }

                _actionsUI.UpdateUI();
            }

            _dragDropHelper.EndDrag();
        }

        private void OnItemFocusExited(ActionMenuItem item)
        {
            // lost focus, cancel the drag if one is in progress
            _dragDropHelper.EndDrag();
        }

        private void OnItemPressed(ButtonEventArgs args)
        {
            if (args.Button is not ActionMenuItem actionMenuItem) return;
            switch (actionMenuItem.Action)
            {
                case ActionPrototype actionPrototype:
                    _actionsComponent.Assignments.AutoPopulate(ActionAssignment.For(actionPrototype.ActionType), _actionsUI.SelectedHotbar);
                    break;
                case ItemActionPrototype itemActionPrototype:
                    _actionsComponent.Assignments.AutoPopulate(ActionAssignment.For(itemActionPrototype.ActionType), _actionsUI.SelectedHotbar);
                    break;
                default:
                    Logger.ErrorS("action", "unexpected action prototype {0}", actionMenuItem.Action);
                    break;
            }

            _actionsUI.UpdateUI();
        }

        private void OnClearButtonPressed(ButtonEventArgs args)
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
                _filterLabel.Text = Loc.GetString("Filters: {0}", string.Join(", ", _filterButton.SelectedLabels));
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

        private bool ActionMatchesFilterTag(BaseActionPrototype action, string tag)
        {
            return tag switch
            {
                AllActionsTag => true,
                GrantedActionsTag => _actionsComponent.IsGranted(action),
                ItemTag => action is ItemActionPrototype,
                NotItemTag => action is ActionPrototype,
                InstantActionTag => action.BehaviorType == BehaviorType.Instant,
                TargetActionTag => action.IsTargetAction,
                ToggleActionTag => action.BehaviorType == BehaviorType.Toggle,
                _ => action.Filters.Contains(tag)
            };
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
            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                {
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                }

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
                var actionItem = new ActionMenuItem(action, OnItemFocusExited);
                _resultsGrid.Children.Add(actionItem);
                actionItem.SetActionState(_actionsComponent.IsGranted(action));
                actionItem.OnButtonDown += OnItemButtonDown;
                actionItem.OnButtonUp += OnItemButtonUp;
                actionItem.OnPressed += OnItemPressed;
            }
        }

        private void ClearList()
        {
            // TODO: Not sure if this unsub is needed if children are all being cleared
            foreach (var actionItem in _resultsGrid.Children)
            {
                ((ActionMenuItem) actionItem).OnPressed -= OnItemPressed;
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
                var actionMenuItem = ((ActionMenuItem) actionItem);
                actionMenuItem.SetActionState(_actionsComponent.IsGranted(actionMenuItem.Action));
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            _dragDropHelper.Update(args.DeltaSeconds);
        }
    }
}
