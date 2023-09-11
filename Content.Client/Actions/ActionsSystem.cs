using System.IO;
using System.Linq;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        public delegate void OnActionReplaced(EntityUid actionId);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        public event Action<EntityUid>? ActionAdded;
        public event Action<EntityUid>? ActionRemoved;
        public event OnActionReplaced? ActionReplaced;
        public event Action? ActionsUpdated;
        public event Action<ActionsComponent>? LinkActions;
        public event Action? UnlinkActions;
        public event Action? ClearAssignments;
        public event Action<List<SlotAssignment>>? AssignSlot;

        /// <summary>
        ///     Queue of entities with <see cref="ActionsComponent"/> that needs to be updated after
        ///     handling a state.
        /// </summary>
        private readonly Queue<EntityUid> _actionHoldersQueue = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleComponentState);
        }

        public override void Dirty(EntityUid? actionId)
        {
            var action = GetActionData(actionId);
            if (_playerManager.LocalPlayer?.ControlledEntity != action?.AttachedEntity)
                return;

            base.Dirty(actionId);
            ActionsUpdated?.Invoke();
        }

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState state)
                return;

            component.Actions.Clear();
            component.Actions.UnionWith(EnsureEntitySet<ActionsComponent>(state.Actions, uid));

            _actionHoldersQueue.Enqueue(uid);
        }

        protected override void AddActionInternal(EntityUid holderId, EntityUid actionId, BaseContainer container, ActionsComponent holder)
        {
            // Sometimes the client receives actions from the server, before predicting that newly added components will add
            // their own shared actions. Just in case those systems ever decided to directly access action properties (e.g.,
            // action.Toggled), we will remove duplicates:
            if (container.Contains(actionId))
            {
                ActionReplaced?.Invoke(actionId);
            }
            else
            {
                base.AddActionInternal(holderId, actionId, container, holder);
            }
        }

        public override void AddAction(EntityUid holderId, EntityUid actionId, EntityUid? provider, ActionsComponent? holder = null, BaseActionComponent? action = null, bool dirty = true, BaseContainer? actionContainer = null)
        {
            if (!Resolve(holderId, ref holder, false))
                return;

            action ??= GetActionData(actionId);
            if (action == null)
            {
                Log.Warning($"No {nameof(BaseActionComponent)} found on entity {actionId}");
                return;
            }

            dirty &= !action.ClientExclusive;
            base.AddAction(holderId, actionId, provider, holder, action, dirty, actionContainer);

            if (holderId == _playerManager.LocalPlayer?.ControlledEntity)
                ActionAdded?.Invoke(actionId);
        }

        public override void RemoveAction(EntityUid holderId, EntityUid? actionId, ActionsComponent? comp = null, BaseActionComponent? action = null, bool dirty = true)
        {
            if (GameTiming.ApplyingState)
                return;

            if (!Resolve(holderId, ref comp, false))
                return;

            if (actionId == null)
                return;

            action ??= GetActionData(actionId);

            if (action is { ClientExclusive: false })
                return;

            dirty &= !action?.ClientExclusive ?? true;
            base.RemoveAction(holderId, actionId, comp, action, dirty);

            if (_playerManager.LocalPlayer?.ControlledEntity != holderId)
                return;

            if (action == null || action.AutoRemove)
                ActionRemoved?.Invoke(actionId.Value);
        }

        public IEnumerable<(EntityUid Id, BaseActionComponent Comp)> GetClientActions()
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not { } user)
                return Enumerable.Empty<(EntityUid, BaseActionComponent)>();

            return GetActions(user);
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
            UnlinkActions?.Invoke();
        }

        public void LinkAllActions(ActionsComponent? actions = null)
        {
             if (_playerManager.LocalPlayer?.ControlledEntity is not { } user ||
                 !Resolve(user, ref actions, false))
             {
                 return;
             }

             LinkActions?.Invoke(actions);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }

        public void TriggerAction(EntityUid actionId, BaseActionComponent action)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not { } user ||
                !TryComp(user, out ActionsComponent? actions))
            {
                return;
            }

            if (action.Provider != null && Deleted(action.Provider))
                return;

            if (action is not InstantActionComponent instantAction)
                return;

            if (action.ClientExclusive)
            {
                if (instantAction.Event != null)
                    instantAction.Event.Performer = user;

                PerformAction(user, actions, actionId, instantAction, instantAction.Event, GameTiming.CurTime);
            }
            else
            {
                var request = new RequestPerformActionEvent(GetNetEntity(actionId));
                EntityManager.RaisePredictiveEvent(request);
            }
        }

        /// <summary>
        ///     Load actions and their toolbar assignments from a file.
        /// </summary>
        public void LoadActionAssignments(string path, bool userData)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not { } user)
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

                var action = _serialization.Read<BaseActionComponent>(actionNode, notNullableOverride: true);
                var actionId = Spawn(null);
                AddComp<Component>(actionId, action);
                AddAction(user, actionId, null);

                if (map.TryGet<ValueDataNode>("name", out var nameNode))
                    _metaData.SetEntityName(actionId, nameNode.Value);

                if (!map.TryGet("assignments", out var assignmentNode))
                    continue;

                var nodeAssignments = _serialization.Read<List<(byte Hotbar, byte Slot)>>(assignmentNode, notNullableOverride: true);

                foreach (var index in nodeAssignments)
                {
                    var assignment = new SlotAssignment(index.Hotbar, index.Slot, actionId);
                    assignments.Add(assignment);
                }
            }

            AssignSlot?.Invoke(assignments);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_actionHoldersQueue.Count == 0)
                return;

            var removed = new List<EntityUid>();
            var added = new List<(EntityUid Id, BaseActionComponent Comp)>();
            var query = GetEntityQuery<ActionsComponent>();
            var queue = new Queue<EntityUid>(_actionHoldersQueue);
            _actionHoldersQueue.Clear();

            while (queue.TryDequeue(out var holderId))
            {
                if (!TryGetContainer(holderId, out var container) || container.ExpectedEntities.Count > 0)
                {
                    _actionHoldersQueue.Enqueue(holderId);
                    continue;
                }

                if (!query.TryGetComponent(holderId, out var holder))
                    continue;

                removed.Clear();
                added.Clear();

                foreach (var (act, data) in holder.OldClientActions.ToList())
                {
                    if (data.ClientExclusive)
                        continue;

                    if (!holder.Actions.Contains(act))
                    {
                        holder.OldClientActions.Remove(act);
                        if (data.AutoRemove)
                            removed.Add(act);
                    }
                }

                // Anything that remains is a new action
                foreach (var newAct in holder.Actions)
                {
                    if (!TryGetActionData(newAct, out var serverData))
                        continue;

                    if (!holder.OldClientActions.ContainsKey(newAct))
                        added.Add((newAct, serverData));

                    holder.OldClientActions[newAct] = new ActionMetaData(serverData.ClientExclusive, serverData.AutoRemove);
                }

                if (_playerManager.LocalPlayer?.ControlledEntity != holderId)
                    return;

                foreach (var action in removed)
                {
                    ActionRemoved?.Invoke(action);
                }

                added.Sort(static (a, b) =>
                {
                    if (a.Comp.Priority != b.Comp.Priority)
                        return a.Comp.Priority - b.Comp.Priority;

                    if (a.Comp.Provider != b.Comp.Provider)
                    {
                        if (a.Comp.Provider == null)
                            return -1;

                        if (b.Comp.Provider == null)
                            return 1;

                        // uid to int casting... it says "Do NOT use this in content". You can't tell me what to do.
                        return (int) a.Comp.Provider - (int) b.Comp.Provider;
                    }

                    return 0;
                });

                foreach (var action in added)
                {
                    ActionAdded?.Invoke(action.Item1);
                }

                ActionsUpdated?.Invoke();
            }
        }

        public record struct SlotAssignment(byte Hotbar, byte Slot, EntityUid ActionId);
    }
}
