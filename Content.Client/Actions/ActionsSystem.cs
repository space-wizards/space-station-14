using Content.Client.Actions.Assignments;
using Content.Client.Actions.UI;
using Content.Client.Construction;
using Content.Client.DragDrop;
using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Client.Outline;
using Content.Client.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Utility;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!; 
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InteractionOutlineSystem _interactionOutline = default!;
        [Dependency] private readonly TargetOutlineSystem _targetOutline = default!;

        // TODO Redo assignments, including allowing permanent user configurable slot assignments.
        /// <summary>
        /// Current assignments for all hotbars / slots for this entity.
        /// </summary>
        public ActionAssignments Assignments = new(Hotbars, Slots);

        public const byte Hotbars = 9;
        public const byte Slots = 10;

        public bool UIDirty;

        public ActionsUI? Ui;
        private EntityUid? _highlightedEntity;

        public override void Initialize()
        {
            base.Initialize();

            // set up hotkeys for hotbar
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenActionsMenu,
                    InputCmdHandler.FromDelegate(_ => ToggleActionsMenu()))
                .Bind(ContentKeyFunctions.Hotbar1,
                    HandleHotbarKeybind(0))
                .Bind(ContentKeyFunctions.Hotbar2,
                    HandleHotbarKeybind(1))
                .Bind(ContentKeyFunctions.Hotbar3,
                    HandleHotbarKeybind(2))
                .Bind(ContentKeyFunctions.Hotbar4,
                    HandleHotbarKeybind(3))
                .Bind(ContentKeyFunctions.Hotbar5,
                    HandleHotbarKeybind(4))
                .Bind(ContentKeyFunctions.Hotbar6,
                    HandleHotbarKeybind(5))
                .Bind(ContentKeyFunctions.Hotbar7,
                    HandleHotbarKeybind(6))
                .Bind(ContentKeyFunctions.Hotbar8,
                    HandleHotbarKeybind(7))
                .Bind(ContentKeyFunctions.Hotbar9,
                    HandleHotbarKeybind(8))
                .Bind(ContentKeyFunctions.Hotbar0,
                    HandleHotbarKeybind(9))
                .Bind(ContentKeyFunctions.Loadout1,
                    HandleChangeHotbarKeybind(0))
                .Bind(ContentKeyFunctions.Loadout2,
                    HandleChangeHotbarKeybind(1))
                .Bind(ContentKeyFunctions.Loadout3,
                    HandleChangeHotbarKeybind(2))
                .Bind(ContentKeyFunctions.Loadout4,
                    HandleChangeHotbarKeybind(3))
                .Bind(ContentKeyFunctions.Loadout5,
                    HandleChangeHotbarKeybind(4))
                .Bind(ContentKeyFunctions.Loadout6,
                    HandleChangeHotbarKeybind(5))
                .Bind(ContentKeyFunctions.Loadout7,
                    HandleChangeHotbarKeybind(6))
                .Bind(ContentKeyFunctions.Loadout8,
                    HandleChangeHotbarKeybind(7))
                .Bind(ContentKeyFunctions.Loadout9,
                    HandleChangeHotbarKeybind(8))
                // when selecting a target, we intercept clicks in the game world, treating them as our target selection. We want to
                // take priority before any other systems handle the click.
                .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(TargetingOnUse, outsidePrediction: true),
                    typeof(ConstructionSystem), typeof(DragDropSystem))
                .BindBefore(EngineKeyFunctions.UIRightClick, new PointerInputCmdHandler(TargetingCancel, outsidePrediction: true))
                .Register<ActionsSystem>();

            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleState);
        }

        protected override void Dirty(ActionType action)
        {
            // Should only ever receive component states for attached player's component.
            // --> lets not bother unnecessarily dirtying and prediction-resetting actions for other players.
            if (action.AttachedEntity != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            base.Dirty(action);
            UIDirty = true;
        }

        private void HandleState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            // Client only needs to care about local player.
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            if (args.Current is not ActionsComponentState state)
                return;

            var serverActions = new SortedSet<ActionType>(state.Actions);

            foreach (var act in component.Actions.ToList())
            {
                if (act.ClientExclusive)
                    continue;

                if (!serverActions.TryGetValue(act, out var serverAct))
                {
                    component.Actions.Remove(act);
                    if (act.AutoRemove && !(Ui?.Locked ?? false))
                        Assignments.Remove(act);
                    continue;
                }

                act.CopyFrom(serverAct);
                serverActions.Remove(serverAct);

                if (act is EntityTargetAction entAct)
                {
                    entAct.Whitelist?.UpdateRegistrations();
                }
            }

            // Anything that remains is a new action
            foreach (var newAct in serverActions)
            {
                if (newAct is EntityTargetAction entAct)
                    entAct.Whitelist?.UpdateRegistrations();

                // We create a new action, not just sorting a reference to the state's action.
                component.Actions.Add((ActionType) newAct.Clone());
            }

            UIDirty = true;
        }

        /// <summary>
        /// Highlights the item slot (inventory or hand) that contains this item
        /// </summary>
        /// <param name="item"></param>
        public void HighlightItemSlot(EntityUid item)
        {
            StopHighlightingItemSlot();

            _highlightedEntity = item;
            _itemSlotManager.HighlightEntity(item);
        }

        /// <summary>
        /// Stops highlighting any item slots we are currently highlighting.
        /// </summary>H
        public void StopHighlightingItemSlot()
        {
            if (_highlightedEntity == null)
                return;

            _itemSlotManager.UnHighlightEntity(_highlightedEntity.Value);
            _highlightedEntity = null;
        }

        protected override void AddActionInternal(ActionsComponent comp, ActionType action)
        {
            // Sometimes the client receives actions from the server, before predicting that newly added components will add
            // their own shared actions. Just in case those systems ever decided to directly access action properties (e.g.,
            // action.Toggled), we will remove duplicates:
            if (comp.Actions.TryGetValue(action, out var existing))
            {
                comp.Actions.Remove(existing);
                Assignments.Replace(existing, action);
            }

            comp.Actions.Add(action);
        }

        public override void AddAction(EntityUid uid, ActionType action, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            if (!Resolve(uid, ref comp, false))
                return;

            base.AddAction(uid, action, provider, comp, dirty);
            UIDirty = true;
        }

        public override void RemoveActions(EntityUid uid, IEnumerable<ActionType> actions, ActionsComponent? comp = null, bool dirty = true)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            if (!Resolve(uid, ref comp, false))
                return;

            base.RemoveActions(uid, actions, comp, dirty);

            foreach (var act in actions)
            {
                if (act.AutoRemove && !(Ui?.Locked ?? false))
                    Assignments.Remove(act);
            }

            UIDirty = true;
        }

        public override void FrameUpdate(float frameTime)
        {
            // avoid updating GUI when doing predictions & resetting state.
            if (UIDirty)
            {
                UIDirty = false;
                UpdateUI();
            }
        }

        /// <summary>
        /// Updates the displayed hotbar (and menu) based on current state of actions.
        /// </summary>
        public void UpdateUI()
        {
            if (Ui == null)
                return;

            foreach (var action in Ui.Component.Actions)
            {
                if (action.AutoPopulate && !Assignments.Assignments.ContainsKey(action))
                    Assignments.AutoPopulate(action, Ui.SelectedHotbar, false);
            }

            // get rid of actions that are no longer available to the user
            foreach (var (action, index) in Assignments.Assignments.ToList())
            {
                if (index.Count == 0)
                {
                    Assignments.Assignments.Remove(action);
                    continue;
                }

                if (action.AutoRemove && !Ui.Locked && !Ui.Component.Actions.Contains(action))
                    Assignments.ClearSlot(index[0].Hotbar, index[0].Slot, false);
            }

            Assignments.PreventAutoPopulate.RemoveWhere(action => !Ui.Component.Actions.Contains(action));

            Ui.UpdateUI();
        }

        public void HandleHotbarKeybind(byte slot, in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            Ui?.HandleHotbarKeybind(slot, args);
        }

        public void HandleChangeHotbarKeybind(byte hotbar, in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            Ui?.HandleChangeHotbarKeybind(hotbar, args);
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, PlayerDetachedEvent args)
        {
            if (Ui == null) return;
            _uiManager.StateRoot.RemoveChild(Ui);
            Ui = null;
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            Assignments = new(Hotbars, Slots);
            Ui = new ActionsUI(this, component);
            _uiManager.StateRoot.AddChild(Ui);
            UIDirty = true;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }

        private PointerInputCmdHandler HandleHotbarKeybind(byte slot)
        {
            // delegate to the ActionsUI, simulating a click on it
            return new((in PointerInputCmdHandler.PointerInputCmdArgs args) =>
                {
                    var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
                    if (playerEntity == null ||
                        !EntityManager.TryGetComponent<ActionsComponent?>(playerEntity.Value, out var actionsComponent)) return false;

                    HandleHotbarKeybind(slot, args);
                    return true;
                }, false);
        }

        private PointerInputCmdHandler HandleChangeHotbarKeybind(byte hotbar)
        {
            // delegate to the ActionsUI, simulating a click on it
            return new((in PointerInputCmdHandler.PointerInputCmdArgs args) =>
                {
                    var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
                    if (!EntityManager.TryGetComponent<ActionsComponent?>(playerEntity, out var actionsComponent)) return false;

                    HandleChangeHotbarKeybind(hotbar, args);
                    return true;
                },
                false);
        }

        private void ToggleActionsMenu()
        {
            Ui?.ToggleActionsMenu();
        }

        /// <summary>
        ///     A action slot was pressed. This either performs the action or toggles the targeting mode.
        /// </summary>
        internal void OnSlotPressed(ActionSlot slot)
        {
            if (Ui == null)
                return;

            if (slot.Action == null || _playerManager.LocalPlayer?.ControlledEntity is not EntityUid user)
                return;

            if (slot.Action.Provider != null && Deleted(slot.Action.Provider))
                return;

            if (slot.Action is not InstantAction instantAction)
            {
                // for target actions, we go into "select target" mode, we don't
                // message the server until we actually pick our target.

                // if we're clicking the same thing we're already targeting for, then we simply cancel
                // targeting
                Ui.ToggleTargeting(slot);
                return;
            }

            if (slot.Action.ClientExclusive)
            {
                if (instantAction.Event != null)
                    instantAction.Event.Performer = user;

                PerformAction(Ui.Component, instantAction, instantAction.Event, GameTiming.CurTime);
            }
            else
            {
                var request = new RequestPerformActionEvent(instantAction);
                EntityManager.RaisePredictiveEvent(request);
            }
        }

        private bool TargetingCancel(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!GameTiming.IsFirstTimePredicted)
                return false;

            // only do something for actual target-based actions
            if (Ui?.SelectingTargetFor?.Action == null)
                return false;

            Ui.StopTargeting();
            return true;
        }

        /// <summary>
        ///     If the user clicked somewhere, and they are currently targeting an action, try and perform it.
        /// </summary>
        private bool TargetingOnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!GameTiming.IsFirstTimePredicted)
                return false;

            // only do something for actual target-based actions
            if (Ui?.SelectingTargetFor?.Action is not TargetedAction action)
                return false;

            if (_playerManager.LocalPlayer?.ControlledEntity is not EntityUid user)
                return false;

            if (!TryComp(user, out ActionsComponent? comp))
                return false;

            // Is the action currently valid?
            if (!action.Enabled
                || action.Charges != null && action.Charges == 0
                || action.Cooldown.HasValue && action.Cooldown.Value.End > GameTiming.CurTime)
            {
                // The user is targeting with this action, but it is not valid. Maybe mark this click as
                // handled and prevent further interactions.
                return !action.InteractOnMiss; 
            }

            switch (action)
            {
                case WorldTargetAction mapTarget:
                    return TryTargetWorld(args, mapTarget, user, comp) || !action.InteractOnMiss;

                case EntityTargetAction entTarget:
                    return TargetEntity(args, entTarget, user, comp) || !action.InteractOnMiss;

                default:
                    Logger.Error($"Unknown targeting action: {action.GetType()}");
                    return false;
            }
        }

        private bool TryTargetWorld(in PointerInputCmdHandler.PointerInputCmdArgs args, WorldTargetAction action, EntityUid user, ActionsComponent actionComp)
        {
            var coords = args.Coordinates.ToMap(EntityManager);

            if (!ValidateWorldTarget(user, coords, action))
            {
                // Invalid target.
                if (action.DeselectOnMiss)
                    Ui?.StopTargeting();

                return false;
            }

            if (action.ClientExclusive)
            {
                if (action.Event != null)
                {
                    action.Event.Target = coords;
                    action.Event.Performer = user;
                }

                PerformAction(actionComp, action, action.Event, GameTiming.CurTime);
            }
            else
                EntityManager.RaisePredictiveEvent(new RequestPerformActionEvent(action, coords));

            if (!action.Repeat)
                Ui?.StopTargeting();

            return true;
        }

        private bool TargetEntity(in PointerInputCmdHandler.PointerInputCmdArgs args, EntityTargetAction action, EntityUid user, ActionsComponent actionComp)
        {
            if (!ValidateEntityTarget(user, args.EntityUid, action))
            {
                if (action.DeselectOnMiss)
                    Ui?.StopTargeting();

                return false;
            }

            if (action.ClientExclusive)
            {
                if (action.Event != null)
                {
                    action.Event.Target = args.EntityUid;
                    action.Event.Performer = user;
                }

                PerformAction(actionComp, action, action.Event, GameTiming.CurTime);
            }
            else
                EntityManager.RaisePredictiveEvent(new RequestPerformActionEvent(action, args.EntityUid));

            if (!action.Repeat)
                Ui?.StopTargeting();

            return true;
        }

        /// <summary>
        ///     Execute convenience functionality for actions (pop-ups, sound, speech)
        /// </summary>
        protected override bool PerformBasicActions(EntityUid user, ActionType action)
        {
            var performedAction = action.Sound != null
                || !string.IsNullOrWhiteSpace(action.UserPopup)
                || !string.IsNullOrWhiteSpace(action.Popup);

            if (!GameTiming.IsFirstTimePredicted)
                return performedAction;

            if (!string.IsNullOrWhiteSpace(action.UserPopup))
            {
                var msg = (!action.Toggled || string.IsNullOrWhiteSpace(action.PopupToggleSuffix))
                    ? Loc.GetString(action.UserPopup)
                    : Loc.GetString(action.UserPopup + action.PopupToggleSuffix);

                _popupSystem.PopupEntity(msg, user);
            }
            else if (!string.IsNullOrWhiteSpace(action.Popup))
            {
                var msg = (!action.Toggled || string.IsNullOrWhiteSpace(action.PopupToggleSuffix))
                    ? Loc.GetString(action.Popup)
                    : Loc.GetString(action.Popup + action.PopupToggleSuffix);

                _popupSystem.PopupEntity(msg, user);
            }

            if (action.Sound != null)
                 SoundSystem.Play(Filter.Local(), action.Sound.GetSound(), user, action.AudioParams);

            return performedAction;
        }

        internal void StopTargeting()
        {
            _targetOutline.Disable();
            _interactionOutline.SetEnabled(true);

            if (!_overlayMan.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay) || handOverlay == null)
                return;

            handOverlay.IconOverride = null;
            handOverlay.EntityOverride = null;
        }

        internal void StartTargeting(TargetedAction action)
        {
            // override "held-item" overlay
            if (action.TargetingIndicator
                && _overlayMan.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay)
                && handOverlay != null)
            {
                if (action.ItemIconStyle == ItemActionIconStyle.BigItem && action.Provider != null)
                {
                    handOverlay.EntityOverride = action.Provider;
                }
                else if (action.Toggled && action.IconOn != null)
                    handOverlay.IconOverride = action.IconOn.Frame0();
                else if (action.Icon != null)
                    handOverlay.IconOverride = action.Icon.Frame0();
            }

            // TODO: allow world-targets to check valid positions. E.g., maybe:
            // - Draw a red/green ghost entity
            // - Add a yes/no checkmark where the HandItemOverlay usually is

            // Highlight valid entity targets
            if (action is not EntityTargetAction entityAction)
                return;

            Func<EntityUid, bool>? predicate = null;

            if (!entityAction.CanTargetSelf)
                predicate = e => e != entityAction.AttachedEntity;

            var range = entityAction.CheckCanAccess ? action.Range : -1;

            _interactionOutline.SetEnabled(false);
            _targetOutline.Enable(range, entityAction.CheckCanAccess, predicate, entityAction.Whitelist, null);
        }

        internal void TryFillSlot(byte hotbar, byte index)
        {
            if (Ui == null)
                return;

            var fillEvent = new FillActionSlotEvent();
            RaiseLocalEvent(Ui.Component.Owner, fillEvent, broadcast: true);

            if (fillEvent.Action == null)
                return;

            fillEvent.Action.ClientExclusive = true;
            fillEvent.Action.Temporary = true;

            Ui.Component.Actions.Add(fillEvent.Action);
            Assignments.AssignSlot(hotbar, index, fillEvent.Action);

            Ui.UpdateUI();
        }

        public void SaveActionAssignments(string path)
        {
            // Disabled until YamlMappingFix's sandbox issues are resolved.

            /*
            // Currently only tested with temporary innate actions (i.e., mapping actions). No guarantee it works with
            // other actions. If its meant to be used for full game state saving/loading, the entity that provides
            // actions needs to keep the same uid.

            var sequence = new SequenceDataNode();

            foreach (var (action, assigns) in Assignments.Assignments)
            {
                var slot = new MappingDataNode();
                slot.Add("action", _serializationManager.WriteValue(action));
                slot.Add("assignments", _serializationManager.WriteValue(assigns));
                sequence.Add(slot);
            }

            using var writer = _resourceManager.UserData.OpenWriteText(new ResourcePath(path).ToRootedPath());
            var stream = new YamlStream { new(sequence.ToSequenceNode()) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
            */
        }

        /// <summary>
        ///     Load actions and their toolbar assignments from a file.
        /// </summary>
        public void LoadActionAssignments(string path, bool userData)
        {
            if (Ui == null)
                return;

            var file = new ResourcePath(path).ToRootedPath();
            TextReader reader = userData
                ? _resourceManager.UserData.OpenText(file)
                : _resourceManager.ContentFileReadText(file);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            if (yamlStream.Documents[0].RootNode.ToDataNode() is not SequenceDataNode sequence)
                return;

            foreach (var (action, assigns) in Assignments.Assignments)
            {
                foreach (var index in assigns)
                {
                    Assignments.ClearSlot(index.Hotbar, index.Slot, true);
                }
            }

            foreach (var entry in sequence.Sequence)
            {
                if (entry is not MappingDataNode map)
                    continue;

                if (!map.TryGet("action", out var actionNode))
                    continue;

                var action = _serializationManager.ReadValueCast<ActionType>(typeof(ActionType), actionNode);
                if (action == null)
                    continue;

                if (Ui.Component.Actions.TryGetValue(action, out var existingAction))
                {
                    existingAction.CopyFrom(action);
                    action = existingAction;
                }
                else
                    Ui.Component.Actions.Add(action);

                if (!map.TryGet("assignments", out var assignmentNode))
                    continue;

                var assignments = _serializationManager.ReadValueCast<List<(byte Hotbar, byte Slot)>>(typeof(List<(byte Hotbar, byte Slot)>), assignmentNode);
                if (assignments == null)
                    continue;

                foreach (var index in assignments)
                {
                    Assignments.AssignSlot(index.Hotbar, index.Slot, action);
                }
            }

            UIDirty = true;
        }
    }
}
