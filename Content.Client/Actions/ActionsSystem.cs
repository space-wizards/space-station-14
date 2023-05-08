using System.IO;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        public delegate void OnActionReplaced(ActionType existing, ActionType action);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

        public event Action<ActionType>? ActionAdded;
        public event Action<ActionType>? ActionRemoved;
        public event OnActionReplaced? ActionReplaced;
        public event Action? ActionsUpdated;
        public event Action<ActionsComponent>? LinkActions;
        public event Action? UnlinkActions;
        public event Action? ClearAssignments;
        public event Action<List<SlotAssignment>>? AssignSlot;

        public ActionsComponent? PlayerActions { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleComponentState);
        }

        public override void Dirty(ActionType action)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity != action.AttachedEntity)
                return;

            base.Dirty(action);
            ActionsUpdated?.Invoke();
        }

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState state)
                return;

            var serverActions = new SortedSet<ActionType>(state.Actions);
            var removed = new List<ActionType>();

            foreach (var act in component.Actions.ToList())
            {
                if (act.ClientExclusive)
                    continue;

                if (!serverActions.TryGetValue(act, out var serverAct))
                {
                    component.Actions.Remove(act);
                    if (act.AutoRemove)
                        removed.Add(act);

                    continue;
                }

                act.CopyFrom(serverAct);
                serverActions.Remove(serverAct);
            }

            var added = new List<ActionType>();

            // Anything that remains is a new action
            foreach (var newAct in serverActions)
            {
                // We create a new action, not just sorting a reference to the state's action.
                var action = (ActionType) newAct.Clone();
                component.Actions.Add(action);
                added.Add(action);
            }

            if (_playerManager.LocalPlayer?.ControlledEntity != uid)
                return;

            foreach (var action in removed)
            {
                ActionRemoved?.Invoke(action);
            }

            foreach (var action in added)
            {
                ActionAdded?.Invoke(action);
            }

            ActionsUpdated?.Invoke();
        }

        protected override void AddActionInternal(ActionsComponent comp, ActionType action)
        {
            // Sometimes the client receives actions from the server, before predicting that newly added components will add
            // their own shared actions. Just in case those systems ever decided to directly access action properties (e.g.,
            // action.Toggled), we will remove duplicates:
            if (comp.Actions.TryGetValue(action, out var existing))
            {
                comp.Actions.Remove(existing);
                ActionReplaced?.Invoke(existing, action);
            }

            comp.Actions.Add(action);
        }

        public override void AddAction(EntityUid uid, ActionType action, EntityUid? provider, ActionsComponent? comp = null, bool dirty = true)
        {
            if (GameTiming.ApplyingState && !action.ClientExclusive)
                return;

            if (!Resolve(uid, ref comp, false))
                return;

            dirty &= !action.ClientExclusive;
            base.AddAction(uid, action, provider, comp, dirty);

            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                ActionAdded?.Invoke(action);
        }

        public override void RemoveAction(EntityUid uid, ActionType action, ActionsComponent? comp = null, bool dirty = true)
        {
            if (GameTiming.ApplyingState && !action.ClientExclusive)
                return;

            if (!Resolve(uid, ref comp, false))
                return;

            dirty &= !action.ClientExclusive;
            base.RemoveAction(uid, action, comp, dirty);

            if (action.AutoRemove && uid == _playerManager.LocalPlayer?.ControlledEntity)
                ActionRemoved?.Invoke(action);
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            LinkAllActions(component);
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, PlayerDetachedEvent? args = null)
        {
            UnlinkAllActions();
        }

        public void UnlinkAllActions()
        {
            PlayerActions = null;
            UnlinkActions?.Invoke();
        }

        public void LinkAllActions(ActionsComponent? actions = null)
        {
             var player = _playerManager.LocalPlayer?.ControlledEntity;
             if (player == null || !Resolve(player.Value, ref actions))
             {
                 return;
             }

             LinkActions?.Invoke(actions);
             PlayerActions = actions;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }

        public void TriggerAction(ActionType? action)
        {
            if (PlayerActions == null || action == null || _playerManager.LocalPlayer?.ControlledEntity is not { Valid: true } user)
                return;

            if (action.Provider != null && Deleted(action.Provider))
                return;

            if (action is not InstantAction instantAction)
            {
                return;
            }

            if (action.ClientExclusive)
            {
                if (instantAction.Event != null)
                    instantAction.Event.Performer = user;

                PerformAction(user, PlayerActions, instantAction, instantAction.Event, GameTiming.CurTime);
            }
            else
            {
                var request = new RequestPerformActionEvent(instantAction);
                EntityManager.RaisePredictiveEvent(request);
            }
        }

        /*public void SaveActionAssignments(string path)
        {

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
        }*/

        /// <summary>
        ///     Load actions and their toolbar assignments from a file.
        /// </summary>
        public void LoadActionAssignments(string path, bool userData)
        {
            if (PlayerActions == null)
                return;

            var file = new ResPath(path).ToRootedPath();
            TextReader reader = userData
                ? _resources.UserData.OpenText(file)
                : _resources.ContentFileReadText(file);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            if (yamlStream.Documents[0].RootNode.ToDataNode() is not SequenceDataNode sequence)
                return;

            ClearAssignments?.Invoke();

            var assignments = new List<SlotAssignment>();

            foreach (var entry in sequence.Sequence)
            {
                if (entry is not MappingDataNode map)
                    continue;

                if (!map.TryGet("action", out var actionNode))
                    continue;

                var action = _serialization.Read<ActionType>(actionNode, notNullableOverride: true);

                if (PlayerActions.Actions.TryGetValue(action, out var existingAction))
                {
                    existingAction.CopyFrom(action);
                    action = existingAction;
                }
                else
                {
                    PlayerActions.Actions.Add(action);
                }

                if (!map.TryGet("assignments", out var assignmentNode))
                    continue;

                var nodeAssignments = _serialization.Read<List<(byte Hotbar, byte Slot)>>(assignmentNode, notNullableOverride: true);

                foreach (var index in nodeAssignments)
                {
                    var assignment = new SlotAssignment(index.Hotbar, index.Slot, action);
                    assignments.Add(assignment);
                }
            }

            AssignSlot?.Invoke(assignments);
        }

        public record struct SlotAssignment(byte Hotbar, byte Slot, ActionType Action);
    }
}
