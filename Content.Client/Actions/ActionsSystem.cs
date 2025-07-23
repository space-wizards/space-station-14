using System.IO;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Mapping;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        public delegate void OnActionReplaced(EntityUid actionId);

        [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        public event Action<EntityUid>? OnActionAdded;
        public event Action<EntityUid>? OnActionRemoved;
        public event Action? ActionsUpdated;
        public event Action<ActionsComponent>? LinkActions;
        public event Action? UnlinkActions;
        public event Action? ClearAssignments;
        public event Action<List<SlotAssignment>>? AssignSlot;

        private readonly List<EntityUid> _removed = new();
        private readonly List<Entity<ActionComponent>> _added = new();

        public static readonly EntProtoId MappingEntityAction = "BaseMappingEntityAction";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActionsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<ActionsComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<ActionComponent, AfterAutoHandleStateEvent>(OnActionAutoHandleState);

            SubscribeLocalEvent<EntityTargetActionComponent, ActionTargetAttemptEvent>(OnEntityTargetAttempt);
            SubscribeLocalEvent<WorldTargetActionComponent, ActionTargetAttemptEvent>(OnWorldTargetAttempt);
        }


        private void OnActionAutoHandleState(Entity<ActionComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            UpdateAction(ent);
        }

        public override void UpdateAction(Entity<ActionComponent> ent)
        {
            // TODO: Decouple this.
            ent.Comp.IconColor = _sharedCharges.GetCurrentCharges(ent.Owner) == 0 ? ent.Comp.DisabledIconColor : ent.Comp.OriginalIconColor;
            base.UpdateAction(ent);
            if (_playerManager.LocalEntity != ent.Comp.AttachedEntity)
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

                if (GetAction(actionId) is {} action)
                    _added.Add(action);
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

        protected override void ActionAdded(Entity<ActionsComponent> performer, Entity<ActionComponent> action)
        {
            if (_playerManager.LocalEntity != performer.Owner)
                return;

            OnActionAdded?.Invoke(action);
            ActionsUpdated?.Invoke();
        }

        protected override void ActionRemoved(Entity<ActionsComponent> performer, Entity<ActionComponent> action)
        {
            if (_playerManager.LocalEntity != performer.Owner)
                return;

            OnActionRemoved?.Invoke(action);
            ActionsUpdated?.Invoke();
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

        public void TriggerAction(Entity<ActionComponent> action)
        {
            if (_playerManager.LocalEntity is not { } user)
                return;

            // TODO: unhardcode this somehow

            if (!HasComp<InstantActionComponent>(action))
                return;

            if (action.Comp.ClientExclusive)
            {
                PerformAction(user, action);
            }
            else
            {
                var request = new RequestPerformActionEvent(GetNetEntity(action));
                RaisePredictiveEvent(request);
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

            var actions = EnsureComp<ActionsComponent>(user);

            ClearAssignments?.Invoke();

            var assignments = new List<SlotAssignment>();
            foreach (var entry in sequence.Sequence)
            {
                if (entry is not MappingDataNode map)
                    continue;

                if (!map.TryGet("assignments", out var assignmentNode))
                    continue;

                var actionId = EntityUid.Invalid;
                if (map.TryGet<ValueDataNode>("action", out var actionNode))
                {
                    var id = new EntProtoId(actionNode.Value);
                    actionId = Spawn(id);
                }
                else if (map.TryGet<ValueDataNode>("entity", out var entityNode))
                {
                    var id = new EntProtoId(entityNode.Value);
                    var proto = _proto.Index(id);
                    actionId = Spawn(MappingEntityAction);
                    SetIcon(actionId, new SpriteSpecifier.EntityPrototype(id));
                    SetEvent(actionId, new StartPlacementActionEvent()
                    {
                        PlacementOption = "SnapgridCenter",
                        EntityType = id
                    });
                    _metaData.SetEntityName(actionId, proto.Name);
                }
                else if (map.TryGet<ValueDataNode>("tileId", out var tileNode))
                {
                    var id = new ProtoId<ContentTileDefinition>(tileNode.Value);
                    var proto = _proto.Index(id);
                    actionId = Spawn(MappingEntityAction);
                    if (proto.Sprite is {} sprite)
                        SetIcon(actionId, new SpriteSpecifier.Texture(sprite));
                    SetEvent(actionId, new StartPlacementActionEvent()
                    {
                        PlacementOption = "AlignTileAny",
                        TileId = id
                    });
                    _metaData.SetEntityName(actionId, Loc.GetString(proto.Name));
                }
                else
                {
                    Log.Error($"Mapping actions from {path} had unknown action data!");
                    continue;
                }

                AddActionDirect((user, actions), actionId);
            }
        }

        private void OnWorldTargetAttempt(Entity<WorldTargetActionComponent> ent, ref ActionTargetAttemptEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            var (uid, comp) = ent;
            var action = args.Action;
            var coords = args.Input.Coordinates;
            var user = args.User;

            if (!ValidateWorldTarget(user, coords, ent))
                return;

            // optionally send the clicked entity too, if it matches its whitelist etc
            // this is the actual entity-world targeting magic
            EntityUid? targetEnt = null;
            if (TryComp<EntityTargetActionComponent>(ent, out var entity) &&
                args.Input.EntityUid != null &&
                ValidateEntityTarget(user, args.Input.EntityUid, (uid, entity)))
            {
                targetEnt = args.Input.EntityUid;
            }

            if (action.ClientExclusive)
            {
                // TODO: abstract away from single event or maybe just RaiseLocalEvent?
                if (comp.Event is {} ev)
                {
                    ev.Target = coords;
                    ev.Entity = targetEnt;
                }

                PerformAction((user, user.Comp), (uid, action));
            }
            else
                RaisePredictiveEvent(new RequestPerformActionEvent(GetNetEntity(uid), GetNetEntity(targetEnt), GetNetCoordinates(coords)));

            args.FoundTarget = true;
        }

        private void OnEntityTargetAttempt(Entity<EntityTargetActionComponent> ent, ref ActionTargetAttemptEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (args.Input.EntityUid is not { Valid: true } entity)
                return;

            // let world target component handle it
            var (uid, comp) = ent;
            if (comp.Event is not {} ev)
            {
                DebugTools.Assert(HasComp<WorldTargetActionComponent>(ent), $"Action {ToPrettyString(ent)} requires WorldTargetActionComponent for entity-world targeting");
                return;
            }

            var action = args.Action;
            var user = args.User;

            if (!ValidateEntityTarget(user, entity, ent))
                return;

            if (action.ClientExclusive)
            {
                ev.Target = entity;

                PerformAction((user, user.Comp), (uid, action));
            }
            else
            {
                RaisePredictiveEvent(new RequestPerformActionEvent(GetNetEntity(uid), GetNetEntity(entity)));
            }

            args.FoundTarget = true;
        }

        public record struct SlotAssignment(byte Hotbar, byte Slot, EntityUid ActionId);
    }
}
