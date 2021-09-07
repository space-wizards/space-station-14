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
using Robust.Shared.Input;
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

        public event EventHandler<PointerInputCmdHandler.PointerInputCmdArgs>? ToggleContextMenu;
        public event EventHandler<bool>? ToggleContainerVisibility;

        private ContextMenuPresenter _contextMenuPresenter = default!;

        // Verb types to be displayed in the context menu.
        private List<Verb> _interactionVerbs = new();
        private List<Verb> _activationVerbs = new();
        private List<Verb> _alternativeVerbs = new();
        private List<Verb> _otherVerbs = new();

        public EntityUid CurrentEntity;
        public ContextMenuPopup? CurrentVerbPopup;
        public ContextMenuPopup? CurrentCategoryPopup;

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

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(HandleOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _contextMenuPresenter?.Dispose();

            CommandBinds.Unregister<VerbSystem>();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ToggleContainerVisibility?.Invoke(this, false);
        }

        private bool HandleOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
            {
                ToggleContextMenu?.Invoke(this, args);
            }
            return true;
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

        public void OpenVerbMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (CurrentVerbPopup != null)
            {
                CloseVerbMenu();
            }

            CurrentEntity = entity.Uid;

            CurrentVerbPopup = new ContextMenuPopup();
            _userInterfaceManager.ModalRoot.AddChild(CurrentVerbPopup);
            CurrentVerbPopup.OnPopupHide += CloseVerbMenu;

            GetAllClientsideVerbs(entity);

            if (!entity.Uid.IsClientSide())
            {
                CurrentVerbPopup.AddToMenu(new Label { Text = Loc.GetString("verb-system-waiting-on-server-text") });
                RaiseNetworkEvent(new RequestServerVerbsEvent(CurrentEntity));
            }

            // Show the menu
            FillVerbPopup(CurrentVerbPopup);
            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            CurrentVerbPopup.Open(box);
        }

        internal void ExecuteServerVerb(EntityUid target, string key)
        {
            RaiseNetworkEvent(new UseVerbEvent(target, key));
        }

        /// <summary>
        ///     Raise events to get a list of all verbs on an entity.
        /// </summary>
        private void GetAllClientsideVerbs(IEntity entity)
        {
            var user = _playerManager.LocalPlayer!.ControlledEntity!;

            // Get primary-interaction verbs
            GetInteractionVerbsEvent interactionVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(entity.Uid, interactionVerbs);
            _interactionVerbs = interactionVerbs.Verbs;

            // Get activation verbs
            GetActivationVerbsEvent activationVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(entity.Uid, activationVerbs);
            _activationVerbs = activationVerbs.Verbs;

            // Get primary-interaction verbs
            GetAlternativeVerbsEvent alternativeVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(entity.Uid, alternativeVerbs);
            _alternativeVerbs = alternativeVerbs.Verbs;

            // Get primary-interaction verbs
            GetOtherVerbsEvent otherVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(entity.Uid, otherVerbs);
            _otherVerbs = otherVerbs.Verbs;
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenVerbMenu(entity, _userInterfaceManager.MousePositionScaled);
        }

        private void HandleVerbResponse(VerbsResponseEvent msg)
        {
            if (CurrentEntity != msg.Entity || CurrentVerbPopup == null)
            {
                return;
            }

            // Merge message verbs with client side verb list
            _interactionVerbs.AddRange(msg.InteractionVerbs);
            _activationVerbs.AddRange(msg.ActivationVerbs);
            _alternativeVerbs.AddRange(msg.AlternativeVerbs);
            _otherVerbs.AddRange(msg.OtherVerbs);

            // Clear currently shown verbs and show new ones
            CurrentVerbPopup.List.DisposeAllChildren();
            FillVerbPopup(CurrentVerbPopup);
            CurrentVerbPopup.InvalidateMeasure();
        }

        private void FillVerbPopup(ContextMenuPopup popup)
        {
            if (CurrentEntity == EntityUid.Invalid)
                return;

            AddVerbList(popup, _interactionVerbs);
            AddVerbList(popup, _activationVerbs);
            AddVerbList(popup, _alternativeVerbs);
            AddVerbList(popup, _otherVerbs);

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
        private void AddVerbList(ContextMenuPopup popup, List<Verb> verbList)
        {
            if (verbList.Count == 0)
                return;

            // Sort verbs by priority or name
            verbList.Sort();

            HashSet<string> listedCategories = new();

            foreach (var verb in verbList)
            {
                if (verb.Category == null)
                {
                    // Lone verb. just create a button for it
                    popup.AddToMenu(new VerbButton(this, verb, CurrentEntity));
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
                        new VerbCategoryButton(this, verb.Category, verbsInCategory, CurrentEntity));
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

                popup.AddToMenu(new VerbButton(this, verb, CurrentEntity));
            }
        }

        public void CloseVerbMenu()
        {
            CurrentVerbPopup?.Dispose();
            CurrentVerbPopup = null;
            CurrentCategoryPopup?.Dispose();
            CurrentCategoryPopup = null;
            CurrentEntity = EntityUid.Invalid;
            _interactionVerbs.Clear();
            _activationVerbs.Clear();
            _alternativeVerbs.Clear();
            _otherVerbs.Clear();
        }
    }
}
