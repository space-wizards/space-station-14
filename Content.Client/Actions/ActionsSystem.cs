using System.IO;
using System.Linq;
using Content.Shared.Actions;
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
        public event OnActionReplaced? ActionReplaced;
        public event Action? ActionsUpdated;
        public event Action<ActionsComponent>? LinkActions;
        public event Action? UnlinkActions;
        public event Action? ClearAssignments;
        public event Action<List<SlotAssignment>>? AssignSlot;

        private readonly List<EntityUid> _removed = new();
        private readonly List<(EntityUid, BaseActionComponent?)> _added = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(HandleComponentState);

            SubscribeLocalEvent<InstantActionComponent, ComponentHandleState>(OnInstantHandleState);
            SubscribeLocalEvent<EntityTargetActionComponent, ComponentHandleState>(OnEntityTargetHandleState);
            SubscribeLocalEvent<WorldTargetActionComponent, ComponentHandleState>(OnWorldTargetHandleState);
        }

        private void OnInstantHandleState(EntityUid uid, InstantActionComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not InstantActionComponentState state)
                return;

            BaseHandleState<InstantActionComponent>(uid, component, state);
        }

        private void OnEntityTargetHandleState(EntityUid uid, EntityTargetActionComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not EntityTargetActionComponentState state)
                return;

            component.Whitelist = state.Whitelist;
            component.CanTargetSelf = state.CanTargetSelf;
            BaseHandleState<EntityTargetActionComponent>(uid, component, state);
        }

        private void OnWorldTargetHandleState(EntityUid uid, WorldTargetActionComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not WorldTargetActionComponentState state)
                return;

            BaseHandleState<WorldTargetActionComponent>(uid, component, state);
        }

        private void BaseHandleState<T>(EntityUid uid, BaseActionComponent component, BaseActionComponentState state) where T : BaseActionComponent
        {
            component.Icon = state.Icon;
            component.IconOn = state.IconOn;
            component.IconColor = state.IconColor;
            component.Keywords = new HashSet<string>(state.Keywords);
            component.Enabled = state.Enabled;
            component.Toggled = state.Toggled;
            component.Cooldown = state.Cooldown;
            component.UseDelay = state.UseDelay;
            component.Charges = state.Charges;
            component.Container = EnsureEntity<T>(state.Container, uid);
            component.EntityIcon = EnsureEntity<T>(state.EntityIcon, uid);
            component.CheckCanInteract = state.CheckCanInteract;
            component.ClientExclusive = state.ClientExclusive;
            component.Priority = state.Priority;
            component.AttachedEntity = EnsureEntity<T>(state.AttachedEntity, uid);
            component.AutoPopulate = state.AutoPopulate;
            component.Temporary = state.Temporary;
            component.ItemIconStyle = state.ItemIconStyle;
            component.Sound = state.Sound;

            if (_playerManager.LocalPlayer?.ControlledEntity == component.AttachedEntity)
                ActionsUpdated?.Invoke();
        }

        protected override void UpdateAction(EntityUid? actionId, BaseActionComponent? action = null)
        {
            if (!ResolveActionData(actionId, ref action))
                return;

            base.UpdateAction(actionId, action);
            if (_playerManager.LocalPlayer?.ControlledEntity != action.AttachedEntity)
                return;

            ActionsUpdated?.Invoke();
        }

        private void HandleComponentState(EntityUid uid, ActionsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActionsComponentState state)
                return;

            _added.Clear();
            _removed.Clear();
            var stateEnts = EnsureEntitySet<ActionsComponent>(state.Actions, uid);
            foreach (var act in component.Actions)
            {
                if (!stateEnts.Contains(act) && !IsClientSide(act))
                    _removed.Add(act);
            }
            component.Actions.ExceptWith(_removed);

            foreach (var actionId in stateEnts)
            {
                if (!actionId.IsValid())
                    continue;

                if (!component.Actions.Add(actionId))
                    continue;

                TryGetActionData(actionId, out var action);
                _added.Add((actionId, action));
            }

            if (_playerManager.LocalPlayer?.ControlledEntity != uid)
                return;

            foreach (var action in _removed)
            {
                OnActionRemoved?.Invoke(action);
            }

            _added.Sort(ActionComparer);

            foreach (var action in _added)
            {
                OnActionAdded?.Invoke(action.Item1);
            }

            ActionsUpdated?.Invoke();
        }

        public static int ActionComparer((EntityUid, BaseActionComponent?) a, (EntityUid, BaseActionComponent?) b)
        {
            var priorityA = a.Item2?.Priority ?? 0;
            var priorityB = b.Item2?.Priority ?? 0;
            if (priorityA != priorityB)
                return priorityA - priorityB;

            priorityA = a.Item2?.Container?.Id ?? 0;
            priorityB = b.Item2?.Container?.Id ?? 0;
            return priorityA - priorityB;
        }

        protected override void ActionAdded(EntityUid performer, EntityUid actionId, ActionsComponent comp,
            BaseActionComponent action)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity != performer)
                return;

            OnActionAdded?.Invoke(actionId);
        }

        protected override void ActionRemoved(EntityUid performer, EntityUid actionId, ActionsComponent comp, BaseActionComponent action)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity != performer)
                return;

            OnActionRemoved?.Invoke(actionId);
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
