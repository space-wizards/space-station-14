using System.Collections.Generic;
using System.Linq;
using Content.Client.Examine;
using Content.Client.Items.Managers;
using Content.Client.Verbs;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
namespace Content.Client.ContextMenu.UI
{
    /// <summary>
    ///     This class handles the displaying of the entity context menu.
    /// </summary>
    /// <remarks>
    ///     In addition to the normal <see cref="ContextMenuPresenter"/> functionality, this also provides functions get
    ///     a list of entities near the mouse position, add them to the context menu grouped by prototypes, and remove
    ///     them from the menu as they move out of sight.
    /// </remarks>
    public sealed partial class EntityMenuPresenter : ContextMenuPresenter
    {
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private readonly VerbSystem _verbSystem;

        /// <summary>
        ///     This maps the currently displayed entities to the actual GUI elements.
        /// </summary>
        /// <remarks>
        ///     This is used remove GUI elements when the entities are deleted. or leave the LOS.
        /// </remarks>
        public Dictionary<IEntity, EntityMenuElement> Elements = new();

        public EntityMenuPresenter(VerbSystem verbSystem) : base()
        {
            IoCManager.InjectDependencies(this);

            _verbSystem = verbSystem;

            _cfg.OnValueChanged(CCVars.EntityMenuGroupingType, OnGroupingChanged, true);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,  new PointerInputCmdHandler(HandleOpenEntityMenu))
                .Register<EntityMenuPresenter>();
        }

        public override void Dispose()
        {
            base.Dispose();
            Elements.Clear();
            CommandBinds.Unregister<EntityMenuPresenter>();
        }

        /// <summary>
        ///     Given a list of entities, sort them into groups and them to a new entity menu.
        /// </summary>
        public void OpenRootMenu(List<IEntity> entities)
        {
            var entitySpriteStates = GroupEntities(entities);
            var orderedStates = entitySpriteStates.ToList();
            orderedStates.Sort((x, y) => string.CompareOrdinal(x.First().Prototype?.Name, y.First().Prototype?.Name));
            Elements.Clear();
            AddToUI(orderedStates);

            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled.Position, (1, 1));
            RootMenu.Open(box);
        }

        public override void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            base.OnKeyBindDown(element, args);
            if (element is not EntityMenuElement entityElement)
                return;

            // get an entity associated with this element
            var entity = entityElement.Entity;
            entity ??= GetFirstEntityOrNull(element.SubMenu);
            if (entity == null)
                return;

            // open verb menu?
            if (args.Function == ContentKeyFunctions.OpenContextMenu)
            {
                _verbSystem.VerbMenu.OpenVerbMenu(entity);
                args.Handle();
                return;
            }

            // do examination?
            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                _systemManager.GetEntitySystem<ExamineSystem>().DoExamine(entity);
                args.Handle();
                return;
            }

            // do some other server-side interaction?
            if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.AltActivateItemInWorld || args.Function == ContentKeyFunctions.Point ||
                args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
            {
                var inputSys = _systemManager.GetEntitySystem<InputSystem>();

                var func = args.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

                var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, funcId,
                    BoundKeyState.Down, entity.Transform.Coordinates, args.PointerLocation, entity.Uid);

                var session = _playerManager.LocalPlayer?.Session;
                if (session != null)
                {
                    inputSys.HandleInputCommand(session, func, message);
                }

                _verbSystem.CloseAllMenus();
                args.Handle();
                return;
            }

            if (_itemSlotManager.OnButtonPressed(args, entity))
            {
                _verbSystem.CloseAllMenus();
            }
        }

        private bool HandleOpenEntityMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State != BoundKeyState.Down)
                return false;

            if (_stateManager.CurrentState is not GameScreenBase)
                return false;

            var coords = args.Coordinates.ToMap(_entityManager);

            if (!_verbSystem.TryGetEntityMenuEntities(coords, out var entities))
                return false;

            OpenRootMenu(entities);
            return true;
        }

        /// <summary>
        ///     Check that entities in the context menu are still visible. If not, remove them from the context menu.
        /// </summary>
        public void Update()
        {
            if (!RootMenu.Visible)
                return;

            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null)
                return;

            // Do we need to do in-range unOccluded checks?
            var ignoreFov = !_eyeManager.CurrentEye.DrawFov ||
                (_verbSystem.Visibility & MenuVisibility.NoFov) == MenuVisibility.NoFov;

            foreach (var entity in Elements.Keys.ToList())
            {
                if (entity.Deleted || !ignoreFov && !player.InRangeUnOccluded(entity))
                    RemoveEntity(entity);
            }
        }

        /// <summary>
        ///     Add menu elements for a list of grouped entities; 
        /// </summary>
        /// <param name="entityGroups"> A list of entity groups. Entities are grouped together based on prototype.</param>
        private void AddToUI(List<List<IEntity>> entityGroups)
        {
            // If there is only a single group. We will just directly list individual entities
            if (entityGroups.Count == 1)
            {
                foreach (var entity in entityGroups[0])
                {
                    var element = new EntityMenuElement(entity);
                    AddElement(RootMenu, element);
                    Elements.TryAdd(entity, element);
                }
                return;
            }

            foreach (var group in entityGroups)
            {
                if (group.Count > 1)
                {
                    AddGroupToUI(group);
                    continue;
                }

                // this group only has a single entity, add a simple menu element
                var element = new EntityMenuElement(group[0]);
                AddElement(RootMenu, element);
                Elements.TryAdd(group[0], element);
            }
            
        }

        /// <summary>
        ///     Given a group of entities, add a menu element that has a pop-up sub-menu listing group members
        /// </summary>
        private void AddGroupToUI(List<IEntity> group)
        {
            EntityMenuElement element = new();
            ContextMenuPopup subMenu = new(this, element);

            foreach (var entity in group)
            {
                var subElement = new EntityMenuElement(entity);
                AddElement(subMenu, subElement);
                Elements.TryAdd(entity, subElement);
            }

            UpdateElement(element);
            AddElement(RootMenu, element);
        }

        /// <summary>
        ///     Remove an entity from the entity context menu.
        /// </summary>
        private void RemoveEntity(IEntity entity)
        {
            // find the element associated with this entity
            if (!Elements.TryGetValue(entity, out var element))
            {
                Logger.Error($"Attempted to remove unknown entity from the entity menu: {entity.Name} ({entity.Uid})");
                return;
            }

            // remove the element
            var parent = element.ParentMenu?.ParentElement;
            element.Dispose();
            Elements.Remove(entity);

            // update any parent elements
            if (parent is EntityMenuElement e)
                UpdateElement(e);

            // if the verb menu is open and targeting this entity, close it.
            if (_verbSystem.VerbMenu.CurrentTarget == entity.Uid)
                _verbSystem.VerbMenu.Close();

            // If this was the last entity, close the entity menu
            if (RootMenu.MenuBody.ChildCount == 0)
                Close();
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

            // Update the entity count & count label
            element.Count = 0;
            foreach (var subElement in element.SubMenu.MenuBody.Children)
            {
                if (subElement is EntityMenuElement entityElement)
                    element.Count += entityElement.Count;
            }
            element.CountLabel.Text = element.Count.ToString();

            if (element.Count == 1)
            {
                // There was only one entity in the sub-menu. So we will just remove the sub-menu and point directly to
                // that entity.
                element.Entity = entity;
                element.SubMenu.Dispose();
                element.SubMenu = null;
                element.CountLabel.Visible = false;
                Elements[entity] = element;
            }

            // update the parent element, so that it's count and entity icon gets updated.
            var parent = element.ParentMenu?.ParentElement;
            if (parent is EntityMenuElement e)
                UpdateElement(e);
        }

        /// <summary>
        ///     Look through a sub-menu and return the first entity.
        /// </summary>
        private IEntity? GetFirstEntityOrNull(ContextMenuPopup? menu)
        {
            if (menu == null)
                return null;

            foreach (var element in menu.MenuBody.Children)
            {
                if (element is not EntityMenuElement entityElement)
                    continue;

                if (entityElement.Entity != null)
                    return entityElement.Entity;

                var entity = GetFirstEntityOrNull(entityElement.SubMenu);
                if (entity != null)
                    return entity;
            }

            return null;
        }

        public override void OpenSubMenu(ContextMenuElement element)
        {
            base.OpenSubMenu(element);

            // In case the verb menu is currently open, ensure that it is shown ABOVE the entity menu.
            if (_verbSystem.VerbMenu.Menus.TryPeek(out var menu) && menu.Visible)
            {
                menu.ParentElement?.ParentMenu?.SetPositionLast();
                menu.SetPositionLast();
            }
        }
    }
}
