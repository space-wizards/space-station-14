using System.IO;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
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

        public event Action<EntityUid>? OnActionAdded;
        public event Action<EntityUid>? OnActionRemoved;
        public event Action? ActionsUpdated;
        public event Action<ActionsComponent>? LinkActions;
        public event Action? UnlinkActions;
        public event Action? ClearAssignments;
        public event Action<List<SlotAssignment>>? AssignSlot;

        private readonly List<EntityUid> _removed = new();
        private readonly List<Entity<ActionComponent?>> _added = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActionsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<ActionComponent, AfterAutoHandleStateEvent>(OnActionAutoHandleState);
        }

        private void OnActionAutoHandleState(Entity<ActionComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            UpdateAction(ent, ent.Comp);
        }

        protected override void UpdateAction(EntityUid? actionId, ActionComponent? action = null)
        {
            if (!ResolveActionData(actionId, ref action))
                return;

            base.UpdateAction(actionId, action);
            if (_playerManager.LocalEntity != action.AttachedEntity)
                return;

            ActionsUpdated?.Invoke();
        }

        private void OnHandleState(Entity<ActionsComponent> ent, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState state)
                return;

            var (uid, comp) = ent;
            _added.Clear();
            _removed.Clear();
            var stateEnts = EnsureEntitySet<ActionsComponent>(state.Actions, uid);
            foreach (var act in comp.Actions)
            {
                if (!stateEnts.Contains(act) && !IsClientSide(act))
                    _removed.Add(act);
            }
            comp.Actions.ExceptWith(_removed);

            foreach (var actionId in stateEnts)
            {
                if (!actionId.IsValid())
                    continue;

                if (!comp.Actions.Add(actionId))
                    continue;

                TryGetActionData(actionId, out var action);
                _added.Add((actionId, action));
            }

            if (_playerManager.LocalEntity != uid)
                return;

            foreach (var action in _removed)
            {
                OnActionRemoved?.Invoke(action);
            }

            _added.Sort(ActionComparer);

            foreach (var action in _added)
            {
                OnActionAdded?.Invoke(action);
            }

            ActionsUpdated?.Invoke();
        }

        public static int ActionComparer(Entity<ActionComponent> a, Entity<ActionComponent> b)
        {
            var priorityA = a.Comp?.Priority ?? 0;
            var priorityB = b.Comp?.Priority ?? 0;
            if (priorityA != priorityB)
                return priorityA - priorityB;

            priorityA = a.Comp?.Container?.Id ?? 0;
            priorityB = b.Comp?.Container?.Id ?? 0;
            return priorityA - priorityB;
        }

        protected override void ActionAdded(EntityUid performer, EntityUid actionId, ActionsComponent comp, ActionComponent action)
        {
            if (_playerManager.LocalEntity != performer)
                return;

            OnActionAdded?.Invoke(actionId);
        }

        protected override void ActionRemoved(EntityUid performer, EntityUid actionId, ActionsComponent comp, ActionComponent action)
        {
            if (_playerManager.LocalEntity != performer)
                return;

            OnActionRemoved?.Invoke(actionId);
        }

        public IEnumerable<Entity<ActionComponent>> GetClientActions()
        {
            if (_playerManager.LocalEntity is not { } user)
                return Enumerable.Empty<Entity<ActionComponent>>();

            return GetActions(user);
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, LocalPlayerAttachedEvent args)
        {
            LinkAllActions(component);
        }

        private void OnPlayerDetached(EntityUid uid, ActionsComponent component, LocalPlayerDetachedEvent? args = null)
        {
            UnlinkAllActions();
        }

        public void UnlinkAllActions()
        {
            UnlinkActions?.Invoke();
        }

        public void LinkAllActions(ActionsComponent? actions = null)
        {
             if (_playerManager.LocalEntity is not { } user ||
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

        public void TriggerAction(EntityUid actionId, ActionComponent action)
        {
            if (_playerManager.LocalEntity is not { } user ||
                !TryComp(user, out ActionsComponent? actions))
            {
                return;
            }

            if (!TryComp<InstantActionComponent>(actionId, out var instantAction))
                return;

            if (action.ClientExclusive)
            {
                if (instantAction.Event != null)
                {
                    instantAction.Event.Performer = user;
                    instantAction.Event.Action = actionId;
                }

                PerformAction(user, actions, actionId, action, instantAction.Event, GameTiming.CurTime);
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
            if (_playerManager.LocalEntity is not { } user)
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

                var action = _serialization.Read<ActionComponent>(actionNode, notNullableOverride: true);
                var actionId = Spawn(null);
                AddComp(actionId, action);
                AddActionDirect(user, actionId);

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

        public record struct SlotAssignment(byte Hotbar, byte Slot, EntityUid ActionId);
    }
}
