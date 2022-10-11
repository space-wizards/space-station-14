using System.IO;
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
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

        public event Action<ActionType>? OnActionAdded;
        public event Action<ActionType>? OnActionRemoved;
        public event Action<ActionsComponent>? OnLinkActions;
        public event Action? OnUnlinkActions;
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

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState currentState)
                return;

            List<ActionType> added = new();
            List<ActionType> removed = new();

            foreach (var actionType in component.Actions)
            {
                if (!currentState.Actions.Contains(actionType))
                {
                    removed.Add(actionType);
                }
            }

            foreach (var serverAction in currentState.Actions)
            {
                if (!component.Actions.TryGetValue(serverAction, out var clientAction))
                {
                    added.Add((ActionType) serverAction.Clone());
                }
                else
                {
                    clientAction.CopyFrom(serverAction);
                }
            }

            foreach (var actionType in added)
            {
                component.Actions.Add(actionType);
                OnActionAdded?.Invoke(actionType);
            }

            foreach (var actionType in removed)
            {
                component.Actions.Remove(actionType);
                OnActionRemoved?.Invoke(actionType);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            OnLinkActions?.Invoke(component);
            PlayerActions = component;
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, PlayerDetachedEvent? args = null)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            OnUnlinkActions?.Invoke();
            PlayerActions = null;
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

                PerformAction(PlayerActions, instantAction, instantAction.Event, GameTiming.CurTime);
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

            var file = new ResourcePath(path).ToRootedPath();
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

                var action = _serialization.Read<ActionType>(actionNode);

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

                var nodeAssignments = _serialization.Read<List<(byte Hotbar, byte Slot)>>(assignmentNode);

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
