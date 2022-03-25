using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Client.DragDrop;
using Content.Client.HUD;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    /// <summary>
    /// Action selection menu, allows filtering and searching over all possible
    /// actions and populating those actions into the hotbar.
    /// </summary>
    public sealed class ActionMenu : DefaultWindow
    {
        // Pre-defined global filters that can be used to select actions based on their properties (as opposed to their
        // own yaml-defined filters).
        // TODO LOC STRINGs
        private const string AllFilter = "all";
        private const string ItemFilter = "item";
        private const string InnateFilter = "innate";
        private const string EnabledFilter = "enabled";
        private const string InstantFilter = "instant";
        private const string TargetedFilter = "targeted";

        private readonly string[] _filters =
        {
            AllFilter,
            ItemFilter,
            InnateFilter,
            EnabledFilter,
            InstantFilter,
            TargetedFilter
        };

        private const int MinSearchLength = 3;
        private static readonly Regex NonAlphanumeric = new Regex(@"\W", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Is an action currently being dragged from this window?
        /// </summary>
        public bool IsDragging => _dragDropHelper.IsDragging;

        private readonly ActionsUI _actionsUI;
        private readonly LineEdit _searchBar;
        private readonly MultiselectOptionButton<string> _filterButton;
        private readonly Label _filterLabel;
        private readonly Button _clearButton;
        private readonly GridContainer _resultsGrid;
        private readonly TextureRect _dragShadow;
        private readonly IGameHud _gameHud;
        private readonly DragDropHelper<ActionMenuItem> _dragDropHelper;
        private readonly IEntityManager _entMan;

        public ActionMenu(ActionsUI actionsUI)
        {
            _actionsUI = actionsUI;
            _gameHud = IoCManager.Resolve<IGameHud>();
            _entMan = IoCManager.Resolve<IEntityManager>();

            Title = Loc.GetString("ui-actionmenu-title");
            MinSize = (320, 300);

            Contents.AddChild(new BoxContainer
            {
	            Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_searchBar = new LineEdit
                            {
                                StyleClasses = { StyleNano.StyleClassActionSearchBox },
                                HorizontalExpand = true,
                                PlaceHolder = Loc.GetString("ui-actionmenu-search-bar-placeholder-text")
                            }),
                            (_filterButton = new MultiselectOptionButton<string>()
                            {
                                Label = Loc.GetString("ui-actionmenu-filter-button")
                            })
                        }
                    },
                    (_clearButton = new Button
                    {
                        Text = Loc.GetString("ui-actionmenu-clear-button"),
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

            foreach (var tag in _filters)
            {
                _filterButton.AddItem( CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tag), tag);
            }

            // default to showing all actions.
            _filterButton.SelectKey(AllFilter);

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
            _dragShadow.Texture = _dragDropHelper.Dragged?.Action?.Icon?.Frame0();
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

                _actionsUI.System.Assignments.AssignSlot(_actionsUI.SelectedHotbar, targetSlot.SlotIndex, _dragDropHelper.Dragged.Action);
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

            _actionsUI.System.Assignments.AutoPopulate(actionMenuItem.Action, _actionsUI.SelectedHotbar);
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

            var matchingActions = _actionsUI.Component.Actions
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
                _filterLabel.Text = Loc.GetString("ui-actionmenu-filter-label",
                                                  ("selectedLabels", string.Join(", ", _filterButton.SelectedLabels)));
            }
        }

        private bool MatchesSearchCriteria(ActionType action, string standardizedSearch,
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

            if (Standardize(action.Name.ToString()).Contains(standardizedSearch))
            {
                return true;
            }

            // search by provider name
            if (action.Provider == null || action.Provider == _actionsUI.Component.Owner)
                return false;

            var name = _entMan.GetComponent<MetaDataComponent>(action.Provider.Value).EntityName;
            return Standardize(name).Contains(standardizedSearch);
        }

        private bool ActionMatchesFilterTag(ActionType action, string tag)
        {
            return tag switch
            {
                EnabledFilter => action.Enabled,
                ItemFilter => action.Provider != null && action.Provider != _actionsUI.Component.Owner,
                InnateFilter => action.Provider == null || action.Provider == _actionsUI.Component.Owner,
                InstantFilter => action is InstantAction,
                TargetedFilter => action is TargetedAction,
                _ => true
            };
        }

        /// <summary>
        /// Standardized form is all lowercase, no non-alphanumeric characters (converted to whitespace),
        /// trimmed, 1 space max per whitespace gap,
        /// and optional spaces between case change
        /// </summary>
        private static string Standardize(string rawText, bool splitOnCaseChange = false)
        {
            rawText ??= string.Empty;

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

        private void PopulateActions(IEnumerable<ActionType> actions)
        {
            ClearList();

            foreach (var action in actions)
            {
                var actionItem = new ActionMenuItem(_actionsUI, action, OnItemFocusExited);
                _resultsGrid.Children.Add(actionItem);
                actionItem.SetActionState(action.Enabled);
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
                actionMenuItem.SetActionState(actionMenuItem.Action.Enabled);
            }

            SearchAndDisplay();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            _dragDropHelper.Update(args.DeltaSeconds);
        }
    }
}
