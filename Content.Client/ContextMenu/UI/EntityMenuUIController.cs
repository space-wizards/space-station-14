using System.Linq;
using System.Numerics;
using Content.Client.CombatMode;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Verbs;
using Content.Client.Verbs.UI;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Profiling;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Client.ContextMenu.UI
{
    /// <summary>
    ///     This class handles the displaying of the entity context menu.
    /// </summary>
    /// <remarks>
    ///     This also provides functions to get
    ///     a list of entities near the mouse position, add them to the context menu grouped by prototypes, and remove
    ///     them from the menu as they move out of sight.
    /// </remarks>
    public sealed partial class EntityMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
    {
        [Dependency] private IEntitySystemManager _systemManager = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IStateManager _stateManager = default!;
        [Dependency] private IInputManager _inputManager = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private IParallelManager _parallel = default!;
        [Dependency] private ProfManager _prof = default!;
        [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private ContextMenuUIController _context = default!;
        [Dependency] private VerbMenuUIController _verb = default!;

        [UISystemDependency] private readonly VerbSystem _verbSystem = default!;
        [UISystemDependency] private readonly ExamineSystem _examineSystem = default!;
        [UISystemDependency] private readonly TransformSystem _xform = default!;
        [UISystemDependency] private readonly CombatModeSystem _combatMode = default!;

        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<SpriteComponent> _spriteQuery;

        private bool _updating;
        private MenuVisibility _menuVisibility;
        private TimeSpan _nextVisibilityUpdate;
        private readonly List<EntityUid> _tempEntityList = new();
        private readonly List<EntityUid> _visibilityRangeChecks = new();
        private readonly List<bool> _visibilityRangeResults = new();
        private VisibilityRangeCheckJob _visibilityRangeCheckJob = default!;
        private const float VirtualizedElementHeight = ContextMenuElement.ElementHeight + 2 * ContextMenuElement.ElementMargin;

        /// <summary>
        ///     This maps the currently displayed entities to the actual GUI elements.
        /// </summary>
        /// <remarks>
        ///     This is used remove GUI elements when the entities are deleted. or leave the LOS.
        /// </remarks>
        public Dictionary<EntityUid, EntityMenuElement> Elements = new();

        public void OnStateEntered(GameplayState state)
        {
            _updating = true;
            _cfg.OnValueChanged(CCVars.EntityMenuGroupingType, OnGroupingChanged, true);
            _context.OnContextKeyEvent += OnKeyBindDown;
            _context.OnBeforeOpenSubMenu += OnBeforeOpenSubMenu;

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.UseSecondary,  new PointerInputCmdHandler(HandleOpenEntityMenu, outsidePrediction: true))
                .Register<EntityMenuUIController>();

            _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            _spriteQuery = _entityManager.GetEntityQuery<SpriteComponent>();
            _visibilityRangeCheckJob = new VisibilityRangeCheckJob(
                _visibilityRangeChecks,
                _visibilityRangeResults,
                _examineSystem,
                _xform);
        }

        public void OnStateExited(GameplayState state)
        {
            _updating = false;
            Elements.Clear();
            _cfg.UnsubValueChanged(CCVars.EntityMenuGroupingType, OnGroupingChanged);
            _context.OnContextKeyEvent -= OnKeyBindDown;
            _context.OnBeforeOpenSubMenu -= OnBeforeOpenSubMenu;
            CommandBinds.Unregister<EntityMenuUIController>();
        }

        /// <summary>
        ///     Given a list of entities, sort them into groups and them to a new entity menu.
        /// </summary>
        public void OpenRootMenu(
            List<EntityUid> entities,
            EntityUid? priorityEntity = null,
            MenuVisibility visibility = MenuVisibility.Default)
        {
            using var _ = _prof.Group("EntityMenu Open root menu");

            // close any old menus first.
            if (_context.RootMenu.Visible)
                _context.Close();

            _menuVisibility = visibility;
            _nextVisibilityUpdate = _gameTiming.CurTime;
            _context.RootMenu.ResetBody();
            _context.RootMenu.SetBody(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 0,
            });

            var orderedStates = GroupEntities(entities);
            var sortableGroups = new List<(List<EntityUid> Group, string Name)>(orderedStates.Count);
            foreach (var group in orderedStates)
            {
                sortableGroups.Add((group, Identity.Name(group[0], _entityManager)));
            }

            sortableGroups.Sort(static (x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCulture));
            for (var i = 0; i < sortableGroups.Count; i++)
            {
                orderedStates[i] = sortableGroups[i].Group;
            }

            if (priorityEntity != null && entities.Contains(priorityEntity.Value))
                PrioritizeEntity(orderedStates, priorityEntity.Value);

            Elements.Clear();
            AddToUI(orderedStates);

            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled.Position, new Vector2(1, 1));
            _context.RootMenu.Open(box);
        }

        public void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            if (element is not EntityMenuElement entityElement)
                return;

            EnsureEntitySubMenu(entityElement);

            // get an entity associated with this element
            var entity = entityElement.Entity;
            entity ??= GetFirstEntityOrNull(element.SubMenu);

            // Deleted() automatically checks for null & existence.
            if (_entityManager.Deleted(entity))
                return;

            // do examination?
            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                _systemManager.GetEntitySystem<ExamineSystem>().DoExamine(entity.Value);
                args.Handle();
                return;
            }

            // do some other server-side interaction?
            if (args.Function == EngineKeyFunctions.Use ||
                args.Function == ContentKeyFunctions.ActivateItemInWorld ||
                args.Function == ContentKeyFunctions.AltActivateItemInWorld ||
                args.Function == ContentKeyFunctions.Point ||
                args.Function == ContentKeyFunctions.TryPullObject ||
                args.Function == ContentKeyFunctions.MovePulledObject)
            {
                var inputSys = _systemManager.GetEntitySystem<InputSystem>();

                var func = args.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

                var message = new ClientFullInputCmdMessage(
                    _gameTiming.CurTick,
                    _gameTiming.TickFraction,
                    funcId)
                {
                    State = BoundKeyState.Down,
                    Coordinates = _entityManager.GetComponent<TransformComponent>(entity.Value).Coordinates,
                    ScreenCoordinates = args.PointerLocation,
                    Uid = entity.Value,
                };

                var session = _playerManager.LocalSession;
                if (session != null)
                {
                    inputSys.HandleInputCommand(session, func, message);
                }

                _context.Close();
                args.Handle();
            }
        }

        private bool HandleOpenEntityMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State != BoundKeyState.Down)
                return false;

            if (_stateManager.CurrentState is not GameplayStateBase)
                return false;

            if (_combatMode.IsInCombatMode(args.Session?.AttachedEntity))
                return false;

            var coords = _xform.ToMapCoordinates(args.Coordinates);

            if (_verbSystem.TryGetEntityMenuEntities(coords, out var entities, out var visibility))
                OpenRootMenu(entities, args.EntityUid, visibility);

            return true;
        }

        /// <summary>
        ///     Move the entity that was directly clicked to the top of the root menu, preserving existing ordering for
        ///     all other entities.
        /// </summary>
        private void PrioritizeEntity(List<List<EntityUid>> entityGroups, EntityUid priorityEntity)
        {
            for (var i = 0; i < entityGroups.Count; i++)
            {
                var group = entityGroups[i];
                var entityIndex = group.IndexOf(priorityEntity);

                if (entityIndex == -1)
                    continue;

                if (entityIndex > 0)
                {
                    group.RemoveAt(entityIndex);
                    group.Insert(0, priorityEntity);
                }

                if (i > 0)
                {
                    entityGroups.RemoveAt(i);
                    entityGroups.Insert(0, group);
                }

                return;
            }
        }

        /// <summary>
        ///     Check that entities in the context menu are still visible. If not, remove them from the context menu.
        /// </summary>
        public override void FrameUpdate(FrameEventArgs args)
        {
            if (!_updating)
                return;

            if (!_context.RootMenu.Visible)
                return;

            if (_playerManager.LocalEntity is not { } player ||
                !player.IsValid())
            {
                return;
            }

            // Throttle it to tickrate because it's HELLA expensive on raycasts.
            if (_gameTiming.CurTime < _nextVisibilityUpdate)
                return;

            using var _ = _prof.Group("Entity Menu Frame update");

            _nextVisibilityUpdate = _gameTiming.CurTime + _gameTiming.TickPeriod;
            _entityManager.TryGetComponent(player, out ExaminerComponent? examiner);
            _xformQuery.TryGetComponent(player, out var playerXform);
            var playerCoords = playerXform == null ? default : _xform.GetMapCoordinates(player, playerXform);

            _tempEntityList.Clear();
            _visibilityRangeChecks.Clear();
            _visibilityRangeResults.Clear();

            foreach (var entity in Elements.Keys)
            {
                _tempEntityList.Add(entity);
            }

            foreach (var entity in _tempEntityList)
            {
                if (!Elements.ContainsKey(entity))
                    continue;

                if (!_xformQuery.TryGetComponent(entity, out var xform))
                {
                    // entity was deleted
                    RemoveEntity(entity);
                    continue;
                }

                if ((_menuVisibility & MenuVisibility.Invisible) == 0
                    && _spriteQuery.TryGetComponent(entity, out var sprite)
                    && !sprite.Visible)
                {
                    RemoveEntity(entity);
                    continue;
                }

                if ((_menuVisibility & MenuVisibility.NoFov) == MenuVisibility.NoFov)
                    continue;

                _visibilityRangeChecks.Add(entity);
                _visibilityRangeResults.Add(false);
            }

            _visibilityRangeCheckJob.Player = player;
            _visibilityRangeCheckJob.PlayerTransform = playerXform;
            _visibilityRangeCheckJob.PlayerCoordinates = playerCoords;
            _visibilityRangeCheckJob.Examiner = examiner;
            _parallel.ProcessNow(_visibilityRangeCheckJob, _visibilityRangeChecks.Count);

            for (var i = 0; i < _visibilityRangeChecks.Count; i++)
            {
                var check = _visibilityRangeChecks[i];
                if (!_visibilityRangeResults[i] && Elements.ContainsKey(check))
                    RemoveEntity(check);
            }
        }

        private sealed class VisibilityRangeCheckJob : IParallelRobustJob
        {
            private readonly List<EntityUid> _checks;
            private readonly List<bool> _results;
            private readonly ExamineSystem _examine;
            private readonly SharedTransformSystem _xformSystem;
            public EntityUid Player;
            public TransformComponent? PlayerTransform;
            public MapCoordinates PlayerCoordinates;
            public ExaminerComponent? Examiner;

            public VisibilityRangeCheckJob(
                List<EntityUid> checks,
                List<bool> results,
                ExamineSystem examine,
                SharedTransformSystem xformSystem)
            {
                _checks = checks;
                _results = results;
                _examine = examine;
                _xformSystem = xformSystem;
            }

            public int BatchSize => 16;

            public void Execute(int index)
            {
                var entity = _checks[index];
                var pos = _xformSystem.GetMapCoordinates(entity);

                _results[index] = _examine.CanExamine(
                    (Player, Examiner, PlayerTransform),
                    pos,
                    examined: entity,
                    examinerCoordinates: PlayerCoordinates);
            }
        }

        /// <summary>
        ///     Add menu elements for a list of grouped entities;
        /// </summary>
        /// <param name="entityGroups"> A list of entity groups. Entities are grouped together based on prototype.</param>
        private void AddToUI(List<List<EntityUid>> entityGroups)
        {
            // If there is only a single group. We will just directly list individual entities
            if (entityGroups.Count == 1)
            {
                var group = entityGroups[0];
                CreateVirtualizedGroupMenu(group, _context.RootMenu);
                return;
            }

            foreach (var group in entityGroups)
            {
                if (group.Count > 1)
                {
                    AddGroupToUI(group);
                }
                else
                {
                    // this group only has a single entity, add a simple menu element
                    AddEntityToMenu(group[0], _context.RootMenu);
                }
            }

        }

        /// <summary>
        ///     Given a group of entities, add a menu element that has a pop-up sub-menu listing group members
        /// </summary>
        private void AddGroupToUI(List<EntityUid> group)
        {
            EntityMenuElement element = new();
            element.SetDeferredGroup(group);
            _context.AddElement(_context.RootMenu, element);
        }

        /// <summary>
        ///     Add the group of entities to the menu
        /// </summary>
        private void AddGroupToMenu(List<EntityUid> group, ContextMenuPopup menu)
        {
            foreach (var entity in group)
            {
                AddEntityToMenu(entity, menu);
            }
        }

        /// <summary>
        ///     Add the entity to the menu
        /// </summary>
        private void AddEntityToMenu(EntityUid entity, ContextMenuPopup menu)
        {
            var element = new EntityMenuElement(entity);
            element.HasDeferredSubMenu = true;
            _context.AddElement(menu, element);
            Elements.TryAdd(entity, element);
        }

        private void OnBeforeOpenSubMenu(ContextMenuElement element)
        {
            if (element is EntityMenuElement entityElement)
                EnsureEntitySubMenu(entityElement);
        }

        private void EnsureEntitySubMenu(EntityMenuElement element)
        {
            if (element.SubMenu != null)
                return;

            if (element.DeferredGroupEntities is { } group)
            {
                element.DeferredGroupEntities = null;
                element.HasDeferredSubMenu = false;

                var subMenu = new ContextMenuPopup(_context, element);
                CreateVirtualizedGroupMenu(group, subMenu);
                return;
            }

            if (element.Entity is not { } entity)
                return;

            var verbMenu = new ContextMenuPopup(_context, element);
            verbMenu.OnPopupOpen += () => _verb.OpenVerbMenu(entity, popup: verbMenu);
            verbMenu.OnPopupHide += verbMenu.MenuBody.RemoveAllChildren;
            element.HasDeferredSubMenu = false;
        }

        private void CreateVirtualizedGroupMenu(List<EntityUid> entities, ContextMenuPopup menu)
        {
            // RootMenu is reused between openings, so discard any offset from its previous body before the virtual
            // list's initial item range is calculated.
            menu.MenuScroll.SetScrollValue(Vector2.Zero);

            var body = new VirtualListContainer
            {
                TotalItemCount = entities.Count,
                Separation = 0,
            };
            menu.SetBody(body);

            var group = new VirtualEntityGroup(entities, body);
            menu.MenuScroll.OnScrolled += () => UpdateVirtualizedGroup(menu, group);
            UpdateVirtualizedGroup(menu, group);
        }

        private void UpdateVirtualizedGroup(ContextMenuPopup menu, VirtualEntityGroup group)
        {
            // Try and get first element height if needed.
            if (group.Body.ItemHeight == null && group.Body.ChildCount != 0)
                group.Body.ItemHeight = group.Body.GetChild(0).DesiredSize.Y;

            // Use the body's arranged position rather than the scroll bar value.
            var itemStride = (group.Body.ItemHeight ?? VirtualizedElementHeight) + group.Body.Separation;
            var start = Math.Max((int) MathF.Floor(-group.Body.Position.Y / itemStride), 0);
            var end = Math.Min(start + ContextMenuPopup.MaxItemsBeforeScroll + 1, group.Entities.Count);
            if (start == group.Start && end == group.End)
                return;

            group.Body.RemoveAllChildren();
            group.Body.ItemOffset = start;
            for (var i = start; i < end; i++)
            {
                var element = new EntityMenuElement(group.Entities[i]) { HasDeferredSubMenu = true };
                _context.AddElement(menu, element, group.Body);
            }

            group.Start = start;
            group.End = end;
        }

        private sealed class VirtualEntityGroup(List<EntityUid> entities, VirtualListContainer body)
        {
            public readonly List<EntityUid> Entities = entities;
            public readonly VirtualListContainer Body = body;
            public int Start = -1;
            public int End = -1;
        }

        /// <summary>
        ///     Remove an entity from the entity context menu.
        /// </summary>
        private void RemoveEntity(EntityUid entity)
        {
            // find the element associated with this entity
            if (!Elements.Remove(entity, out var element))
            {
                Log.Error($"Attempted to remove unknown entity from the entity menu: {_entityManager.GetComponent<MetaDataComponent>(entity).EntityName} ({entity})");
                return;
            }

            // remove the element
            var parent = element.ParentMenu?.ParentElement;
            element.Orphan();

            // update any parent elements
            if (parent is EntityMenuElement e)
                UpdateElement(e);

            // If this was the last entity, close the entity menu
            if (_context.RootMenu.Body.ChildCount == 0)
                _context.Close();
        }

        /// <summary>
        ///     Update the information displayed by a menu element.
        /// </summary>
        /// <remarks>
        ///     This is called when initializing elements or after an element was removed from a sub-menu.
        /// </remarks>
        private void UpdateElement(EntityMenuElement element)
        {
            if (element.SubMenu == null)
                return;

            // Get the first entity in the sub-menus
            var entity = GetFirstEntityOrNull(element.SubMenu);
            if (entity == null)
            {
                // This whole element has no associated entities. We should remove it
                element.Orphan();
                return;
            }

            element.UpdateEntity(entity);
            element.UpdateCount();

            if (element.Count == 1)
            {
                // There was only one entity in the sub-menu. So we will just remove the sub-menu and point directly to
                // that entity.
                element.Entity = entity;
                element.SubMenu.Orphan();
                element.SubMenu = null;
                Elements[entity.Value] = element;
            }

            // update the parent element, so that it's count and entity icon gets updated.
            var parent = element.ParentMenu?.ParentElement;
            if (parent is EntityMenuElement e)
                UpdateElement(e);
        }

        /// <summary>
        ///     Recursively look through a sub-menu and return the first entity.
        /// </summary>
        private EntityUid? GetFirstEntityOrNull(ContextMenuPopup? menu)
        {
            if (menu == null)
                return null;

            foreach (var element in menu.Body.Children)
            {
                if (element is not EntityMenuElement entityElement)
                    continue;

                if (entityElement.Entity != null)
                {
                    if (!_entityManager.Deleted(entityElement.Entity))
                        return entityElement.Entity;
                    continue;
                }

                // if the element has no entity, its a group of entities with another attached sub-menu.
                var entity = GetFirstEntityOrNull(entityElement.SubMenu);
                if (entity != null)
                    return entity;
            }

            return null;
        }
    }
}
