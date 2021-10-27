using System;
using System.Linq;
using System.Threading;
using Content.Client.Examine;
using Content.Client.Interactable;
using Content.Client.Items.Managers;
using Content.Client.Verbs;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Interaction.Helpers;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Timer = Robust.Shared.Timing.Timer;
namespace Content.Client.ContextMenu.UI
{
    public class ContextMenuPresenter : IDisposable
    {
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);
        private CancellationTokenSource? _cancelHover;

        private readonly IContextMenuView _contextMenuView;
        private readonly VerbSystem _verbSystem;

        private MapCoordinates _mapCoordinates;

        public ContextMenuPresenter(VerbSystem verbSystem)
        {
            IoCManager.InjectDependencies(this);

            _verbSystem = verbSystem;

            _contextMenuView = new ContextMenuView();
            _contextMenuView.OnKeyBindDownSingle += OnKeyBindDownSingle;
            _contextMenuView.OnMouseEnteredSingle += OnMouseEnteredSingle;
            _contextMenuView.OnMouseExitedSingle += OnMouseExitedSingle;
            _contextMenuView.OnMouseHoveringSingle += OnMouseHoveringSingle;

            _contextMenuView.OnKeyBindDownStack += OnKeyBindDownStack;
            _contextMenuView.OnMouseEnteredStack += OnMouseEnteredStack;

            _contextMenuView.OnExitedTree += OnExitedTree;
            _contextMenuView.OnCloseRootMenu += OnCloseRootMenu;
            _contextMenuView.OnCloseChildMenu += OnCloseChildMenu;

            _cfg.OnValueChanged(CCVars.ContextMenuGroupingType, _contextMenuView.OnGroupingContextMenuChanged, true);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,  new PointerInputCmdHandler(HandleOpenContextMenu))
                .Register<ContextMenuPresenter>();
        }

        #region View Events
        private void OnCloseChildMenu(object? sender, int depth)
        {
            _contextMenuView.CloseContextPopups(depth);
        }

        private void OnCloseRootMenu(object? sender, EventArgs e)
        {
            _contextMenuView.CloseContextPopups();
        }

        private void OnExitedTree(object? sender, ContextMenuElement e)
        {
           _contextMenuView.UpdateParents(e);
        }

        private void OnMouseEnteredStack(object? sender, StackContextElement e)
        {
            var realGlobalPosition = e.GlobalPosition;

            _cancelHover?.Cancel();
            _cancelHover = new();

            Timer.Spawn(HoverDelay, () =>
            {
                if (_contextMenuView.Menus.Count == 0)
                {
                    return;
                }

                OnCloseChildMenu(sender, e.ParentMenu?.Depth ?? 0);

                var filteredEntities = e.ContextEntities.Where(entity => !entity.Deleted);
                if (filteredEntities.Any())
                {
                    _contextMenuView.AddChildMenu(filteredEntities, realGlobalPosition, e);
                }
            }, _cancelHover.Token);
        }

        private void OnKeyBindDownStack(object? sender, (GUIBoundKeyEventArgs, StackContextElement) e)
        {
            var (args, stack) = e;
            var firstEntity = stack.ContextEntities.FirstOrDefault(ent => !ent.Deleted);

            if (firstEntity == null) return;

            if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.AltActivateItemInWorld || args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
            {
                var inputSys = _systemManager.GetEntitySystem<InputSystem>();

                var func = args.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

                var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, funcId,
                    BoundKeyState.Down, firstEntity.Transform.Coordinates, args.PointerLocation, firstEntity.Uid);

                var session = _playerManager.LocalPlayer?.Session;
                if (session != null)
                {
                    inputSys.HandleInputCommand(session, func, message);
                }
                CloseAllMenus();
                args.Handle();
                return;
            }

            if (_itemSlotManager.OnButtonPressed(args, firstEntity))
            {
                CloseAllMenus();
            }
        }

        private void OnMouseHoveringSingle(object? sender, SingleContextElement e)
        {
            if (!e.DrawOutline) return;

            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.ControlledEntity != null)
            {
                var inRange =
                    localPlayer.InRangeUnobstructed(e.ContextEntity, ignoreInsideBlocker: true);

                // BUG: This assumes that the main viewport is the viewport that the context menu is active on.
                // This is not necessarily true but we currently have no way to find the viewport (reliably)
                // from the input event.
                //
                // This might be particularly important in the future with a more advanced mapping mode.
                var renderScale = _eyeManager.MainViewport.GetRenderScale();
                e.OutlineComponent?.UpdateInRange(inRange, renderScale);
            }
        }

        private void OnMouseEnteredSingle(object? sender, SingleContextElement e)
        {
            // close other pop-ups after a short delay
            _cancelHover?.Cancel();
            _cancelHover = new();

            Timer.Spawn(HoverDelay, () =>
            {
                if (_contextMenuView.Menus.Count == 0)
                {
                    return;
                }

                OnCloseChildMenu(sender, e.ParentMenu?.Depth ?? 0);

            }, _cancelHover.Token);


            var entity = e.ContextEntity;

            OnCloseChildMenu(sender, e.ParentMenu?.Depth ?? 0);

            if (entity.Deleted) return;

            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.ControlledEntity == null) return;

            var renderScale = _eyeManager.MainViewport.GetRenderScale();
            e.OutlineComponent?.OnMouseEnter(localPlayer.InRangeUnobstructed(entity, ignoreInsideBlocker: true), renderScale);
            if (e.SpriteComp != null)
            {
                e.SpriteComp.DrawDepth = (int) DrawDepth.HighlightedItems;
            }
            e.DrawOutline = true;
        }

        private void OnMouseExitedSingle(object? sender, SingleContextElement e)
        {
            if (!e.ContextEntity.Deleted)
            {
                if (e.SpriteComp != null)
                {
                    e.SpriteComp.DrawDepth = e.OriginalDrawDepth;
                }
                e.OutlineComponent?.OnMouseLeave();
            }
            e.DrawOutline = false;
        }

        private void OnKeyBindDownSingle(object? sender, (GUIBoundKeyEventArgs, SingleContextElement) valueTuple)
        {
            var (args, single) = valueTuple;
            var entity = single.ContextEntity;
             if (args.Function == ContentKeyFunctions.OpenContextMenu)
             {
                 _verbSystem.OnContextButtonPressed(entity);
                 args.Handle();
                 return;
             }

             if (args.Function == ContentKeyFunctions.ExamineEntity)
             {
                 _systemManager.GetEntitySystem<ExamineSystem>().DoExamine(entity);
                 args.Handle();
                 return;
             }

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

                 CloseAllMenus();
                 args.Handle();
                 return;
             }

             if (_itemSlotManager.OnButtonPressed(args, single.ContextEntity))
             {
                 CloseAllMenus();
             }
        }
        #endregion

        #region Model Updates
        private bool HandleOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State != BoundKeyState.Down)
            {
                return false;
            }

            if (_stateManager.CurrentState is not GameScreenBase)
            {
                return false;
            }

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null)
            {
                return false;
            }

            _mapCoordinates = args.Coordinates.ToMap(_entityManager);

            if (!_verbSystem.TryGetContextEntities(player, _mapCoordinates, out var entities, ignoreVisibility: _verbSystem.CanSeeAllContext))
                return false;

            // do we need to do visiblity checks?
            if (_verbSystem.CanSeeAllContext)
            {
                _contextMenuView.AddRootMenu(entities);
                return true;
            }

            //visibility checks
            player.TryGetContainer(out var playerContainer);
            foreach (var entity in entities.ToList())
            {
                if (!entity.TryGetComponent(out ISpriteComponent? spriteComponent) ||
                    !spriteComponent.Visible ||
                    !CanSeeContainerCheck(entity, playerContainer))
                {
                    entities.Remove(entity);
                }
            }

            if (entities.Count == 0)
                return false;

            _contextMenuView.AddRootMenu(entities);
            return true;
        }

        /// <summary>
        ///     Can the player see the entity through any entity containers?
        /// </summary>
        /// <remarks>
        ///     This is similar to <see cref="ContainerHelpers.IsInSameOrParentContainer()"/>, except that we do not
        ///     allow the player to be the "parent" container and we allow for see-through containers (display cases). 
        /// </remarks>
        private bool CanSeeContainerCheck(IEntity entity, IContainer? playerContainer)
        {
            // is the player inside this entity?
            if (playerContainer?.Owner == entity)
                return true;

            entity.TryGetContainer(out var entityContainer);

            // are they in the same container (or none?)
            if (playerContainer == entityContainer)
                return true;

            // Is the entity in a display case?
            if (playerContainer == null && entityContainer!.ShowContents)
                return true;

            return false;
        }

        /// <summary>
        ///     Check that entities in the context menu are still visible. If not, remove them from the context menu.
        /// </summary>
        public void Update()
        {
            if (_contextMenuView.Elements.Count == 0)
                return;

            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null)
                return;

            foreach (var entity in _contextMenuView.Elements.Keys.ToList())
            {
                if (entity.Deleted || !_verbSystem.CanSeeAllContext && !player.InRangeUnOccluded(entity))
                {
                    _contextMenuView.RemoveEntity(entity);
                    if (_verbSystem.CurrentTarget == entity.Uid)
                        _verbSystem.CloseVerbMenu();
                }
            }
        }
        #endregion

        public void CloseAllMenus()
        {
            _contextMenuView.CloseContextPopups();
            _verbSystem.CloseVerbMenu();
        }

        public void Dispose()
        {
            _contextMenuView.OnKeyBindDownSingle -= OnKeyBindDownSingle;
            _contextMenuView.OnMouseEnteredSingle -= OnMouseEnteredSingle;
            _contextMenuView.OnMouseExitedSingle -= OnMouseExitedSingle;
            _contextMenuView.OnMouseHoveringSingle -= OnMouseHoveringSingle;

            _contextMenuView.OnKeyBindDownStack -= OnKeyBindDownStack;
            _contextMenuView.OnMouseEnteredStack -= OnMouseEnteredStack;

            _contextMenuView.OnExitedTree -= OnExitedTree;
            _contextMenuView.OnCloseRootMenu -= OnCloseRootMenu;
            _contextMenuView.OnCloseChildMenu -= OnCloseChildMenu;

            CommandBinds.Unregister<ContextMenuPresenter>();
        }
    }
}
