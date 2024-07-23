using System.Linq;
using System.Numerics;
using Content.Client.CombatMode;
using Content.Client.ContextMenu.UI;
using Content.Client.Gameplay;
using Content.Shared.Input;
using Content.Shared.Verbs;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client.Verbs.UI
{
    /// <summary>
    ///     This class handles the displaying of the verb menu.
    /// </summary>
    /// <remarks>
    ///     In addition to the normal <see cref="ContextMenuUIController"/> functionality, this also provides functions
    ///     open a verb menu for a given entity, add verbs to it, and add server-verbs when the server response is
    ///     received.
    /// </remarks>
    public sealed class VerbMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ContextMenuUIController _context = default!;

        [UISystemDependency] private readonly CombatModeSystem _combatMode = default!;
        [UISystemDependency] private readonly VerbSystem _verbSystem = default!;

        public NetEntity CurrentTarget;
        public SortedSet<Verb> CurrentVerbs = new();
        public List<VerbCategory> ExtraCategories = new();

        /// <summary>
        ///     Separate from <see cref="ContextMenuUIController.RootMenu"/>, since we can open a verb menu as a submenu
        ///     of an entity menu element. If that happens, we need to be aware and close it properly.
        /// </summary>
        public ContextMenuPopup? OpenMenu = null;

        public void OnStateEntered(GameplayState state)
        {
            _context.OnContextKeyEvent += OnKeyBindDown;
            _context.OnContextClosed += Close;
            _verbSystem.OnVerbsResponse += HandleVerbsResponse;
        }

        public void OnStateExited(GameplayState state)
        {
            _context.OnContextKeyEvent -= OnKeyBindDown;
            _context.OnContextClosed -= Close;
            if (_verbSystem != null)
                _verbSystem.OnVerbsResponse -= HandleVerbsResponse;
            Close();
        }

        /// <summary>
        ///     Open a verb menu and fill it with verbs applicable to the given target entity.
        /// </summary>
        /// <param name="target">Entity to get verbs on.</param>
        /// <param name="force">Used to force showing all verbs (mostly for admins).</param>
        /// <param name="popup">
        ///     If this is not null, verbs will be placed into the given popup instead.
        /// </param>
        public void OpenVerbMenu(EntityUid target, bool force = false, ContextMenuPopup? popup=null)
        {
            DebugTools.Assert(target.IsValid());
            OpenVerbMenu(EntityManager.GetNetEntity(target), force, popup);
        }

        /// <summary>
        ///     Open a verb menu and fill it with verbs applicable to the given target entity.
        /// </summary>
        /// <param name="target">Entity to get verbs on.</param>
        /// <param name="force">Used to force showing all verbs. Only works on networked entities if the user is an admin.</param>
        /// <param name="popup">
        ///     If this is not null, verbs will be placed into the given popup instead.
        /// </param>
        public void OpenVerbMenu(NetEntity target, bool force = false, ContextMenuPopup? popup=null)
        {
            DebugTools.Assert(target.IsValid());
            if (_playerManager.LocalEntity is not {Valid: true} user)
                return;

            if (!force && _combatMode.IsInCombatMode(user))
                return;

            Close();

            var menu = popup ?? _context.RootMenu;
            menu.MenuBody.DisposeAllChildren();

            CurrentTarget = target;
            CurrentVerbs = _verbSystem.GetVerbs(target, user, Verb.VerbTypes, out ExtraCategories, force);
            OpenMenu = menu;

            // Fill in client-side verbs.
            FillVerbPopup(menu);

            // if popup isn't null (ie we are opening out of an entity menu element),
            // assume that that is going to handle opening the submenu properly
            if (popup != null)
                return;

            // Show the menu at mouse pos
            menu.SetPositionLast();
            var box = UIBox2.FromDimensions(UIManager.MousePositionScaled.Position, new Vector2(1, 1));
            menu.Open(box);
        }

        /// <summary>
        ///     Fill the verb pop-up using the verbs stored in <see cref="CurrentVerbs"/>
        /// </summary>
        private void FillVerbPopup(ContextMenuPopup popup)
        {
            HashSet<string> listedCategories = new();
            var extras = new ValueList<string>(ExtraCategories.Count);

            foreach (var cat in ExtraCategories)
            {
                extras.Add(cat.Text);
            }

            foreach (var verb in CurrentVerbs)
            {
                if (verb.Category == null)
                {
                    var element = new VerbMenuElement(verb);
                    _context.AddElement(popup, element);
                }
                // Add the category if it's not an extra (this is to avoid shuffling if we're filling from server verbs response).
                else if (!extras.Contains(verb.Category.Text) && listedCategories.Add(verb.Category.Text))
                    AddVerbCategory(verb.Category, popup);
            }

            foreach (var category in ExtraCategories)
            {
                if (listedCategories.Add(category.Text))
                    AddVerbCategory(category, popup);
            }

            popup.InvalidateMeasure();
        }

        /// <summary>
        ///     Add a verb category button to the pop-up
        /// </summary>
        public void AddVerbCategory(VerbCategory category, ContextMenuPopup popup)
        {
            // Get a list of the verbs in this category
            List<Verb> verbsInCategory = new();
            var drawIcons = false;
            foreach (var verb in CurrentVerbs)
            {
                if (verb.Category?.Text == category.Text)
                {
                    verbsInCategory.Add(verb);
                    drawIcons = drawIcons || verb.Icon != null || verb.IconEntity != null;
                }
            }

            if (verbsInCategory.Count == 0 && !ExtraCategories.Contains(category))
                return;

            var style = verbsInCategory.FirstOrDefault()?.TextStyleClass ?? Verb.DefaultTextStyleClass;
            var element = new VerbMenuElement(category, style);
            _context.AddElement(popup, element);

            // Create the pop-up that appears when hovering over this element
            element.SubMenu = new ContextMenuPopup(_context, element);
            foreach (var verb in verbsInCategory)
            {
                var subElement = new VerbMenuElement(verb)
                {
                    IconVisible = drawIcons,
                    TextVisible = !category.IconsOnly
                };
                _context.AddElement(element.SubMenu, subElement);
            }

            element.SubMenu.MenuBody.Columns = category.Columns;
        }

        /// <summary>
        ///     Add verbs from the server to <see cref="CurrentVerbs"/> and update the verb menu.
        /// </summary>
        public void AddServerVerbs(List<Verb>? verbs, ContextMenuPopup popup)
        {
            popup.MenuBody.DisposeAllChildren();

            // Verbs may be null if the server does not think we can see the target entity. This **should** not happen.
            if (verbs == null)
            {
                // remove "waiting for server..." and inform user that something went wrong.
                _context.AddElement(popup, new ContextMenuElement(Loc.GetString("verb-system-null-server-response")));
                return;
            }

            CurrentVerbs.UnionWith(verbs);
            FillVerbPopup(popup);
        }

        public void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            if (args.Function != EngineKeyFunctions.Use && args.Function != ContentKeyFunctions.ActivateItemInWorld)
                return;

            if (element is not VerbMenuElement verbElement)
            {
                if (element is not ConfirmationMenuElement confElement)
                    return;

                args.Handle();
                ExecuteVerb(confElement.Verb);
                return;
            }

            args.Handle();
            var verb = verbElement.Verb;

            if (verb == null)
            {
                // The user probably clicked on a verb category.
                // If there's only one verb in the category, then it will act as if they clicked on that verb.
                // Otherwise it opens the category menu.

                if (verbElement.SubMenu == null || verbElement.SubMenu.ChildCount == 0)
                    return;

                if (verbElement.SubMenu.MenuBody.ChildCount != 1
                    || verbElement.SubMenu.MenuBody.Children.First() is not VerbMenuElement verbMenuElement)
                {
                    _context.OpenSubMenu(verbElement);
                    return;
                }

                verb = verbMenuElement.Verb;

                if (verb == null)
                    return;
            }

#if DEBUG
            // No confirmation pop-ups in debug mode.
            ExecuteVerb(verb);
#else
            if (!verb.ConfirmationPopup)
            {
                ExecuteVerb(verb);
                return;
            }

            if (verbElement.SubMenu == null)
            {
                var popupElement = new ConfirmationMenuElement(verb, "Confirm");
                verbElement.SubMenu = new ContextMenuPopup(_context, verbElement);
                _context.AddElement(verbElement.SubMenu, popupElement);
            }

            _context.OpenSubMenu(verbElement);
#endif
        }

        private void Close()
        {
            if (OpenMenu == null)
                return;

            OpenMenu.Close();
            OpenMenu = null;
        }

        private void HandleVerbsResponse(VerbsResponseEvent msg)
        {
            if (OpenMenu == null || !OpenMenu.Visible || CurrentTarget != msg.Entity)
                return;

            AddServerVerbs(msg.Verbs, OpenMenu);
        }

        private void ExecuteVerb(Verb verb)
        {
            UIManager.ClickSound();
            _verbSystem.ExecuteVerb(CurrentTarget, verb);

            if (verb.CloseMenu ?? verb.CloseMenuDefault)
                _context.Close();
        }
    }
}
