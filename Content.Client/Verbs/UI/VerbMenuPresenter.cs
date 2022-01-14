using System.Collections.Generic;
using System.Linq;
using Content.Client.ContextMenu.UI;
using Content.Shared.Input;
using Content.Shared.Verbs;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Verbs.UI
{
    /// <summary>
    ///     This class handles the displaying of the verb menu.
    /// </summary>
    /// <remarks>
    ///     In addition to the normal <see cref="ContextMenuPresenter"/> functionality, this also provides functions
    ///     open a verb menu for a given entity, add verbs to it, and add server-verbs when the server response is
    ///     received.
    /// </remarks>
    public sealed class VerbMenuPresenter : ContextMenuPresenter
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private readonly VerbSystem _verbSystem;

        public EntityUid CurrentTarget;
        public Dictionary<VerbType, SortedSet<Verb>> CurrentVerbs = new();

        public VerbMenuPresenter(VerbSystem verbSystem)
        {
            IoCManager.InjectDependencies(this);
            _verbSystem = verbSystem;
        }

        /// <summary>
        ///     Open a verb menu and fill it work verbs applicable to the given target entity.
        /// </summary>
        /// <param name="target">Entity to get verbs on.</param>
        /// <param name="force">Used to force showing all verbs (mostly for admins).</param>
        public void OpenVerbMenu(EntityUid target, bool force = false)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} user)
                return;

            Close();

            CurrentTarget = target;
            CurrentVerbs = _verbSystem.GetVerbs(target, user, VerbType.All, force);

            if (!target.IsClientSide())
            {
                AddElement(RootMenu, new ContextMenuElement(Loc.GetString("verb-system-waiting-on-server-text")));
            }

            // Show the menu
            FillVerbPopup();
            RootMenu.SetPositionLast();
            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled.Position, (1, 1));
            RootMenu.Open(box);
        }

        /// <summary>
        ///     Fill the verb pop-up using the verbs stored in <see cref="CurrentVerbs"/>
        /// </summary>
        private void FillVerbPopup()
        {
            if (RootMenu == null)
                return;

            // Add verbs to pop-up, grouped by type. Order determined by how types are defined VerbTypes
            var types = CurrentVerbs.Keys.ToList();
            types.Sort();
            foreach (var type in types)
            {
                if (!CurrentVerbs.TryGetValue(type, out var verbs))
                    continue;

                HashSet<string> listedCategories = new();
                foreach (var verb in verbs)
                {
                    if (verb.Category == null)
                    {
                        var element = new VerbMenuElement(verb, type);
                        AddElement(RootMenu, element);
                    }

                    else if (listedCategories.Add(verb.Category.Text))
                        AddVerbCategory(verb.Category, verbs, type);
                }
            }

            RootMenu.InvalidateMeasure();
        }

        /// <summary>
        ///     Add a verb category button to the pop-up
        /// </summary>
        public void AddVerbCategory(VerbCategory category, SortedSet<Verb> verbs, VerbType type)
        {
            // Get a list of the verbs in this category
            List<Verb> verbsInCategory = new();
            var drawIcons = false;
            foreach (var verb in verbs)
            {
                if (verb.Category?.Text == category.Text)
                {
                    verbsInCategory.Add(verb);
                    drawIcons = drawIcons || verb.Icon != null;
                }
            }

            if (verbsInCategory.Count == 0)
                return;

            var element = new VerbMenuElement(category, type);
            AddElement(RootMenu, element);

            // Create the pop-up that appears when hovering over this element
            element.SubMenu = new ContextMenuPopup(this, element);
            foreach (var verb in verbsInCategory)
            {
                var subElement = new VerbMenuElement(verb, type)
                {
                    IconVisible = drawIcons,
                    TextVisible = !category.IconsOnly
                };
                AddElement(element.SubMenu, subElement);
            }

            if (category.IconsOnly)
                element.SubMenu.MenuBody.Orientation = LayoutOrientation.Horizontal;
        }

        /// <summary>
        ///     Add verbs from the server to <see cref="CurrentVerbs"/> and update the verb menu.
        /// </summary>
        public void AddServerVerbs(Dictionary<VerbType, List<Verb>>? verbs)
        {
            RootMenu.MenuBody.DisposeAllChildren();

            // Verbs may be null if the server does not think we can see the target entity. This **should** not happen.
            if (verbs == null)
            {
                // remove "waiting for server..." and inform user that something went wrong.
                AddElement(RootMenu, new ContextMenuElement(Loc.GetString("verb-system-null-server-response")));
                return;
            }

            // Add the new server-side verbs.
            foreach (var (verbType, verbSet) in verbs)
            {
                if (!CurrentVerbs.TryAdd(verbType, new SortedSet<Verb>(verbSet)))
                {
                    CurrentVerbs[verbType].UnionWith(verbSet);
                }
            }

            FillVerbPopup();
        }

        public override void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            if (args.Function != EngineKeyFunctions.Use && args.Function != ContentKeyFunctions.ActivateItemInWorld)
                return;

            if (element is not VerbMenuElement verbElement)
            {
                if (element is not ConfirmationMenuElement confElement)
                    return;

                args.Handle();
                ExecuteVerb(confElement.Verb, confElement.Type);
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
                    OpenSubMenu(verbElement);
                    return;
                }

                verb = verbMenuElement.Verb;

                if (verb == null)
                    return;
            }

            if (verb.ConfirmationPopup)
            {
                if (verbElement.SubMenu == null)
                {
                    var popupElement = new ConfirmationMenuElement(verb, "Confirm", verbElement.Type);
                    verbElement.SubMenu = new ContextMenuPopup(this, verbElement);
                    AddElement(verbElement.SubMenu, popupElement);
                }

                OpenSubMenu(verbElement);
            }
            else
            {
                ExecuteVerb(verb, verbElement.Type);
            }
        }

        private void ExecuteVerb(Verb verb, VerbType verbType)
        {
            _verbSystem.ExecuteVerb(CurrentTarget, verb, verbType);
            if (verb.CloseMenu)
                _verbSystem.CloseAllMenus();
        }
    }
}
