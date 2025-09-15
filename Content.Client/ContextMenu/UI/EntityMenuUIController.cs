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
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
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
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly ContextMenuUIController _context = default!;
        [Dependency] private readonly VerbMenuUIController _verb = default!;

        [UISystemDependency] private readonly VerbSystem _verbSystem = default!;
        [UISystemDependency] private readonly ExamineSystem _examineSystem = default!;
        [UISystemDependency] private readonly TransformSystem _xform = default!;
        [UISystemDependency] private readonly CombatModeSystem _combatMode = default!;

        private bool _updating;

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

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.UseSecondary,  new PointerInputCmdHandler(HandleOpenEntityMenu, outsidePrediction: true))
                .Register<EntityMenuUIController>();
        }

        public void OnStateExited(GameplayState state)
        {
            _updating = false;
            Elements.Clear();
            _cfg.UnsubValueChanged(CCVars.EntityMenuGroupingType, OnGroupingChanged);
            _context.OnContextKeyEvent -= OnKeyBindDown;
            CommandBinds.Unregister<EntityMenuUIController>();
        }

        /// <summary>
        ///     Given a list of entities, sort them into groups and them to a new entity menu.
        /// </summary>
        public void OpenRootMenu(List<EntityUid> entities)
        {
            // close any old menus first.
            if (_context.RootMenu.Visible)
                _context.Close();

            var entitySpriteStates = GroupEntities(entities);
            var orderedStates = entitySpriteStates.ToList();
            orderedStates.Sort((x, y) => string.Compare(
                Identity.Name(x.First(), _entityManager),
                Identity.Name(y.First(), _entityManager),
                StringComparison.CurrentCulture));
            Elements.Clear();
            AddToUI(orderedStates);

            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled.Position, new Vector2(1, 1));
            _context.RootMenu.Open(box);
        }

        public void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            if (element is not EntityMenuElement entityElement)
                return;

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

            if (_verbSystem.TryGetEntityMenuEntities(coords, out var entities))
                OpenRootMenu(entities);

            return true;
        }

        /// <summary>
        ///     Check that entities in the context menu are still visible. If not, remove them from the context menu.
        /// </summary>
        public override void FrameUpdate(FrameEventArgs args)
        {
            if (!_updating || _context.RootMenu == null)
                return;

            if (!_context.RootMenu.Visible)
                return;

            if (_playerManager.LocalEntity is not { } player ||
                !player.IsValid())
                return;

            // Do we need to do in-range unOccluded checks?
            var visibility = _verbSystem.Visibility;

            if (!_eyeManager.CurrentEye.DrawFov)
            {
                visibility &= ~MenuVisibility.NoFov;
            }

            var ev = new MenuVisibilityEvent()
            {
                Visibility = visibility,
            };

            _entityManager.EventBus.RaiseLocalEvent(player, ref ev);
            visibility = ev.Visibility;

            _entityManager.TryGetComponent(player, out ExaminerComponent? examiner);
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

            foreach (var entity in Elements.Keys.ToList())
            {
                if (!xformQuery.TryGetComponent(entity, out var xform))
                {
                    // entity was deleted
                    RemoveEntity(entity);
                    continue;
                }

                if ((visibility & MenuVisibility.NoFov) == MenuVisibility.NoFov)
                    continue;

                var pos = new MapCoordinates(_xform.GetWorldPosition(xform, xformQuery), xform.MapID);

                if (!_examineSystem.CanExamine(player, pos, e => e == player || e == entity, entity, examiner))
                    RemoveEntity(entity);
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
                AddGroupToMenu(entityGroups[0], _context.RootMenu);
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
            ContextMenuPopup subMenu = new(_context, element);

            AddGroupToMenu(group, subMenu);

            UpdateElement(element);
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
            element.SubMenu = new ContextMenuPopup(_context, element);
            element.SubMenu.OnPopupOpen += () => _verb.OpenVerbMenu(entity, popup: element.SubMenu);
            element.SubMenu.OnPopupHide += element.SubMenu.MenuBody.DisposeAllChildren;
            _context.AddElement(menu, element);
            Elements.TryAdd(entity, element);
        }

        /// <summary>
        ///     Remove an entity from the entity context menu.
        /// </summary>
        private void RemoveEntity(EntityUid entity)
        {
            // find the element associated with this entity
            if (!Elements.TryGetValue(entity, out var element))
            {
                Log.Error($"Attempted to remove unknown entity from the entity menu: {_entityManager.GetComponent<MetaDataComponent>(entity).EntityName} ({entity})");
                return;
            }

            // remove the element
            var parent = element.ParentMenu?.ParentElement;
            element.Dispose();
            Elements.Remove(entity);

            // update any parent elements
            if (parent is EntityMenuElement e)
                UpdateElement(e);

            // If this was the last entity, close the entity menu
            if (_context.RootMenu.MenuBody.ChildCount == 0)
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
                element.Dispose();
                return;
            }

            element.UpdateEntity(entity);
            element.UpdateCount();

            if (element.Count == 1)
            {
                // There was only one entity in the sub-menu. So we will just remove the sub-menu and point directly to
                // that entity.
                element.Entity = entity;
                element.SubMenu.Dispose();
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

            foreach (var element in menu.MenuBody.Children)
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
