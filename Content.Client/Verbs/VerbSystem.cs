using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.ContextMenu.UI;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event EventHandler<bool>? ToggleContainerVisibility;

        private ContextMenuPresenter _contextMenuPresenter = default!;

        public EntityUid CurrentTarget;
        public ContextMenuPopup? CurrentVerbPopup;
        public ContextMenuPopup? CurrentCategoryPopup;
        public Dictionary<VerbType, List<Verb>> CurrentVerbs = new();

        // TODO VERBS Move presenter out of the system
        // TODO VERBS Separate the rest of the UI from the logic
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);
            SubscribeNetworkEvent<PlayerContainerVisibilityMessage>(HandleContainerVisibilityMessage);

            _contextMenuPresenter = new ContextMenuPresenter(this);
            SubscribeLocalEvent<MoveEvent>(_contextMenuPresenter.HandleMoveEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _contextMenuPresenter?.Dispose();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ToggleContainerVisibility?.Invoke(this, false);
        }

        private void HandleContainerVisibilityMessage(PlayerContainerVisibilityMessage ev)
        {
            ToggleContainerVisibility?.Invoke(this, ev.CanSeeThrough);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _contextMenuPresenter?.Update();
        }

        public void OpenVerbMenu(IEntity target, ScreenCoordinates screenCoordinates)
        {
            if (CurrentVerbPopup != null)
            {
                CloseVerbMenu();
            }

            var user = _playerManager.LocalPlayer?.ControlledEntity;
            if (user == null)
                return;

            CurrentTarget = target.Uid;

            CurrentVerbPopup = new ContextMenuPopup();
            _userInterfaceManager.ModalRoot.AddChild(CurrentVerbPopup);
            CurrentVerbPopup.OnPopupHide += CloseVerbMenu;

            CurrentVerbs = GetVerbs(target, user, VerbType.All);

            if (!target.Uid.IsClientSide())
            {
                CurrentVerbPopup.AddToMenu(new Label { Text = Loc.GetString("verb-system-waiting-on-server-text") });
                RaiseNetworkEvent(new RequestServerVerbsEvent(CurrentTarget, VerbType.All));
            }

            // Show the menu
            FillVerbPopup(CurrentVerbPopup);
            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            CurrentVerbPopup.Open(box);
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenVerbMenu(entity, _userInterfaceManager.MousePositionScaled);
        }

        private void HandleVerbResponse(VerbsResponseEvent msg)
        {
            if (CurrentTarget != msg.Entity || CurrentVerbPopup == null)
            {
                return;
            }

            // Add verbs, if the server gave us any. Note that either way we need to update the pop-up to get rid of the
            // "waiting for server...".
            if (msg.Verbs !=null)
            {
                foreach (var entry in msg.Verbs)
                {
                    if (!CurrentVerbs.TryAdd(entry.Key, entry.Value))
                    {
                        CurrentVerbs[entry.Key].AddRange(entry.Value);
                    }
                }
            }

            // Clear currently shown verbs and show new ones
            CurrentVerbPopup.List.DisposeAllChildren();
            FillVerbPopup(CurrentVerbPopup);
            CurrentVerbPopup.InvalidateMeasure();
        }

        private void FillVerbPopup(ContextMenuPopup popup)
        {
            if (CurrentTarget == EntityUid.Invalid)
                return;

            // Add verbs to pop-up, grouped by type. Order determined by how types are defined VerbTypes
            var types = CurrentVerbs.Keys.ToList();
            types.Sort();
            foreach (var type in types)
            {
                AddVerbList(popup, type);
            }

            // Were the verb lists empty?
            if (popup.List.ChildCount == 0)
            {
                
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("verb-system-no-verbs-text") });
                popup.AddChild(panel);
            }
        }

        /// <summary>
        ///     Add a list of verbs to a BoxContainer. Iterates over the given verbs list and creates GUI buttons.
        /// </summary>
        private void AddVerbList(ContextMenuPopup popup, VerbType type)
        {
            if (!CurrentVerbs.TryGetValue(type, out var verbList) || verbList.Count == 0)
                return;

            // Sort verbs by priority or name
            verbList.Sort();

            HashSet<string> listedCategories = new();

            foreach (var verb in verbList)
            {
                if (verb.Category == null)
                {
                    // Lone verb. just create a button for it
                    popup.AddToMenu(new VerbButton(this, verb, type, CurrentTarget));
                    continue;
                }

                if (listedCategories.Contains(verb.Category.Text))
                {
                    // This verb was already included in a verb-category button
                    continue;
                }

                // This is a new verb category. add a button for it

                var verbsInCategory = verbList.Where(v => v.Category?.Text == verb.Category.Text);

                if (verbsInCategory.Count() > 1 || !verb.Category.Contractible)
                {
                    popup.AddToMenu(
                        new VerbCategoryButton(this, verb.Category, verbsInCategory, type, CurrentTarget));
                    listedCategories.Add(verb.Category.Text);
                    continue;
                }

                // This category only contains a single verb, and the category is flagged as Contractible/collapsible.
                // So we add a single modified verb button instead of a verb group button.
                verb.Icon ??= verb.Category.Icon;

                if (verb.Text == string.Empty)
                    verb.Text = verb.Category.Text;
                else
                    verb.Text = verb.Category.Text + " " + verb.Text;

                popup.AddToMenu(new VerbButton(this, verb, type, CurrentTarget));
            }
        }

        public void CloseVerbMenu()
        {
            CurrentVerbPopup?.Dispose();
            CurrentVerbPopup = null;
            CurrentCategoryPopup?.Dispose();
            CurrentCategoryPopup = null;
            CurrentTarget = EntityUid.Invalid;
            CurrentVerbs.Clear();
        }
    }
}
