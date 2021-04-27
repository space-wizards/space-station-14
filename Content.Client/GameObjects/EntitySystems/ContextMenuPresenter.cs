#nullable enable
using System;
using System.Linq;
using System.Threading;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.UserInterface.ContextMenu;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Input;
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
using Timer = Robust.Shared.Timing.Timer;
namespace Content.Client.GameObjects.EntitySystems
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

        private readonly IContextMenuView _contextMenuView;
        private readonly VerbSystem _verbSystem;

        private bool _playerCanSeeThroughContainers;

        private MapCoordinates _mapCoordinates;
        private CancellationTokenSource? _cancellationTokenSource;

        public ContextMenuPresenter(VerbSystem verbSystem)
        {
            IoCManager.InjectDependencies(this);

            _verbSystem = verbSystem;
            _verbSystem.ToggleContextMenu += SystemOnToggleContextMenu;
            _verbSystem.ToggleContainerVisibility += SystemOnToggleContainerVisibility;

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

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();

            Timer.Spawn(e.HoverDelay, () =>
            {
                _verbSystem.CloseGroupMenu();

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
            }, _cancellationTokenSource.Token);
        }

        private void OnKeyBindDownStack(object? sender, (GUIBoundKeyEventArgs, StackContextElement) e)
        {
            var (args, stack) = e;
            var firstEntity = stack.ContextEntities.FirstOrDefault(ent => !ent.Deleted);

            if (firstEntity == null) return;

            if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
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
            _cancellationTokenSource?.Cancel();

            var entity = e.ContextEntity;
            _verbSystem.CloseGroupMenu();

            OnCloseChildMenu(sender, e.ParentMenu?.Depth ?? 0);

            if (entity.Deleted) return;

            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.ControlledEntity == null) return;

            var renderScale = _eyeManager.MainViewport.GetRenderScale();
            e.OutlineComponent?.OnMouseEnter(localPlayer.InRangeUnobstructed(entity, ignoreInsideBlocker: true), renderScale);
            if (e.SpriteComp != null)
            {
                e.SpriteComp.DrawDepth = (int) Shared.GameObjects.DrawDepth.HighlightedItems;
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

             if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.Point ||
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
        private void SystemOnToggleContainerVisibility(object? sender, bool args)
        {
            _playerCanSeeThroughContainers = args;
        }

        private void SystemOnToggleContextMenu(object? sender, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_stateManager.CurrentState is not GameScreenBase)
            {
                return;
            }

            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (playerEntity == null)
            {
                return;
            }

            _mapCoordinates = args.Coordinates.ToMap(_entityManager);
            if (!_verbSystem.TryGetContextEntities(playerEntity, _mapCoordinates, out var entities))
            {
                return;
            }

            entities = entities.Where(CanSeeOnContextMenu).ToList();
            if (entities.Count > 0)
            {
                _contextMenuView.AddRootMenu(entities);
            }
        }

        public void HandleMoveEvent(MoveEvent ev)
        {
            if (_contextMenuView.Elements.Count == 0) return;
            var entity = ev.Sender;
            if (_contextMenuView.Elements.ContainsKey(entity))
            {
                if (!entity.Transform.MapPosition.InRange(_mapCoordinates, 1.0f))
                {
                    _contextMenuView.RemoveEntity(entity);
                }
            }
        }

        public void Update()
        {
            if (_contextMenuView.Elements.Count == 0) return;

            foreach (var entity in _contextMenuView.Elements.Keys.ToList())
            {
                if (entity.Deleted || !_playerCanSeeThroughContainers && entity.IsInContainer())
                {
                    _contextMenuView.RemoveEntity(entity);
                }
            }
        }
        #endregion

        private bool CanSeeOnContextMenu(IEntity entity)
        {
            if (!entity.TryGetComponent(out ISpriteComponent? spriteComponent) || !spriteComponent.Visible)
            {
                return false;
            }

            if (entity.GetAllComponents<IShowContextMenu>().Any(s => !s.ShowContextMenu(entity)))
            {
                return false;
            }

            return _playerCanSeeThroughContainers || !entity.TryGetContainer(out var container) || container.ShowContents;
        }

        private void CloseAllMenus()
        {
            _contextMenuView.CloseContextPopups();
            _verbSystem.CloseGroupMenu();
            _verbSystem.CloseVerbMenu();
        }

        public void Dispose()
        {
            _verbSystem.ToggleContextMenu -= SystemOnToggleContextMenu;
            _verbSystem.ToggleContainerVisibility -= SystemOnToggleContainerVisibility;

            _contextMenuView.OnKeyBindDownSingle -= OnKeyBindDownSingle;
            _contextMenuView.OnMouseEnteredSingle -= OnMouseEnteredSingle;
            _contextMenuView.OnMouseExitedSingle -= OnMouseExitedSingle;
            _contextMenuView.OnMouseHoveringSingle -= OnMouseHoveringSingle;

            _contextMenuView.OnKeyBindDownStack -= OnKeyBindDownStack;
            _contextMenuView.OnMouseEnteredStack -= OnMouseEnteredStack;

            _contextMenuView.OnExitedTree -= OnExitedTree;
            _contextMenuView.OnCloseRootMenu -= OnCloseRootMenu;
            _contextMenuView.OnCloseChildMenu -= OnCloseChildMenu;
        }
    }
}
