using System.Collections.Generic;
using System.Linq;
using Content.Client.ContextMenu.UI;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
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

        public ContextMenuPresenter ContextMenuPresenter = default!;

        public EntityUid CurrentTarget;
        public ContextMenuPopup? CurrentVerbPopup;
        public ContextMenuPopup? CurrentCategoryPopup;
        public Dictionary<VerbType, SortedSet<Verb>> CurrentVerbs = new();

        /// <summary>
        ///     Whether to show all entities on the context menu.
        /// </summary>
        /// <remarks>
        ///     Verb execution will only be affected if the server also agrees that this player can see the target
        ///     entity.
        /// </remarks>
        public bool CanSeeAllContext = false;

        // TODO VERBS Move presenter out of the system
        // TODO VERBS Separate the rest of the UI from the logic
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);
            SubscribeNetworkEvent<SetSeeAllContextEvent>(SetSeeAllContext);

            ContextMenuPresenter = new ContextMenuPresenter(this);
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            ContextMenuPresenter.CloseAllMenus();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ContextMenuPresenter?.Dispose();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ContextMenuPresenter?.Update();
        }

        private void SetSeeAllContext(SetSeeAllContextEvent args)
        {
            CanSeeAllContext = args.CanSeeAllContext;
        }

        /// <summary>
        ///     Execute actions associated with the given verb. If there are no defined actions, this will instead ask
        ///     the server to run the given verb.
        /// </summary>
        public void TryExecuteVerb(Verb verb, EntityUid target, VerbType verbType)
        {
            if (!TryExecuteVerb(verb))
                RaiseNetworkEvent(new TryExecuteVerbEvent(target, verb, verbType));
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

            // This **should** not happen.
            if (msg.Verbs == null)
            {
                // update "waiting for server...".
                CurrentVerbPopup.List.DisposeAllChildren();
                CurrentVerbPopup.AddToMenu(new Label { Text = Loc.GetString("verb-system-null-server-response") });
                FillVerbPopup(CurrentVerbPopup);
                return;
            }

            // Add the new server-side verbs.
            foreach (var (verbType, verbSet) in msg.Verbs)
            {
                SortedSet<Verb>  sortedVerbs = new (verbSet);
                if (!CurrentVerbs.TryAdd(verbType, sortedVerbs))
                {
                    CurrentVerbs[verbType].UnionWith(sortedVerbs);
                }
            }

            // Clear currently shown verbs and show new ones
            CurrentVerbPopup.List.DisposeAllChildren();
            FillVerbPopup(CurrentVerbPopup);
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
                AddVerbSet(popup, type);
            }

            // Were the verb lists empty?
            if (popup.List.ChildCount == 0)
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("verb-system-no-verbs-text") });
                popup.AddChild(panel);
            }

            popup.InvalidateMeasure();
        }

        /// <summary>
        ///     Add a list of verbs to a BoxContainer. Iterates over the given verbs list and creates GUI buttons.
        /// </summary>
        private void AddVerbSet(ContextMenuPopup popup, VerbType type)
        {
            if (!CurrentVerbs.TryGetValue(type, out var verbSet) || verbSet.Count == 0)
                return;

            HashSet<string> listedCategories = new();

            foreach (var verb in verbSet)
            {
                if (verb.Category == null)
                {
                    // Lone verb without a category. just create a button for it
                    popup.AddToMenu(new VerbButton(this, verb, type, CurrentTarget));
                    continue;
                }

                if (listedCategories.Contains(verb.Category.Text))
                {
                    // This verb was already included in a verb-category button added by a previous verb
                    continue;
                }

                // Get the verbs in the category
                var verbsInCategory = verbSet.Where(v => v.Category?.Text == verb.Category.Text);

                popup.AddToMenu(
                    new VerbCategoryButton(this, verb.Category, verbsInCategory, type, CurrentTarget));
                listedCategories.Add(verb.Category.Text);
                continue;
             
            }
        }

        public void CloseVerbMenu()
        {
            if (CurrentVerbPopup != null)
            {
                CurrentVerbPopup.OnPopupHide -= CloseVerbMenu;
                CurrentVerbPopup.Dispose();
                CurrentVerbPopup = null;
            }

            CurrentCategoryPopup?.Dispose();
            CurrentCategoryPopup = null;
            CurrentTarget = EntityUid.Invalid;
            CurrentVerbs.Clear();
        }
    }
}
