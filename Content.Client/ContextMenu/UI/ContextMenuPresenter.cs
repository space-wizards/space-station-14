using System;
using System.Linq;
using System.Threading;
using Content.Client.Examine;
using Content.Client.Interactable;
using Content.Client.Items.Managers;
using Content.Client.Verbs;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Examine;
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

        private bool _playerCanSeeThroughContainers;

        private MapCoordinates _mapCoordinates;

        public ContextMenuPresenter(VerbSystem verbSystem)
        {
            IoCManager.InjectDependencies(this);

            _verbSystem = verbSystem;
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

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                new PointerInputCmdHandler(HandleOpenContextMenu))
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
        private void SystemOnToggleContainerVisibility(object? sender, bool args)
        {
            _playerCanSeeThroughContainers = args;
        }

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

            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (playerEntity == null)
            {
                return false;
            }

            _mapCoordinates = args.Coordinates.ToMap(_entityManager);
            if (!_verbSystem.TryGetContextEntities(playerEntity, _mapCoordinates, out var entities))
            {
                return false;
            }

            // Only include entities that are able to be shown on the context menu. Also exclude entities that the user
            // cannot see the center of. This last part is what the sever checks later on, and will help prevent confusing
            // behavior.
            entities.RemoveAll(e => !CanSeeOnContextMenu(e) || !playerEntity.InRangeUnOccluded(e, ExamineSystemShared.ExamineRange));

            if (entities.Count == 0)
            {
                return false;
            }

            _contextMenuView.AddRootMenu(entities);
            return true;
        }

        /// <summary>
        ///     Check that entities in the context menu are still visible. If not, remove them from the context menu.
        /// </summary>
        public void HandleMoveEvent(ref MoveEvent ev)
        {
            if (_contextMenuView.Elements.Count == 0) return;
            var movingEntityy = ev.Sender;

            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null)
                return;

            // Did the player move? if yes, re-check that the user can still see the listed entities
            if (movingEntityy == player)
            {
                foreach (var listedEntity in _contextMenuView.Elements.Keys)
                {
                    if (!ExamineSystem.InRangeUnOccluded(player, listedEntity, ExamineSystem.ExamineRange, null))
                    {
                        _contextMenuView.RemoveEntity(listedEntity);
                        if (_verbSystem.CurrentTarget == listedEntity.Uid)
                            _verbSystem.CloseVerbMenu();
                    }
                }
            }
            // Did one of the listed entities move out of range?
            else if (_contextMenuView.Elements.ContainsKey(movingEntityy))
            {
                if (!ExamineSystem.InRangeUnOccluded(player, movingEntityy, ExamineSystem.ExamineRange, null))
                {
                    _contextMenuView.RemoveEntity(movingEntityy);
                    if (_verbSystem.CurrentTarget == movingEntityy.Uid)
                        _verbSystem.CloseVerbMenu();
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
                    if (_verbSystem.CurrentTarget == entity.Uid)
                        _verbSystem.CloseVerbMenu();
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

        public void CloseAllMenus()
        {
            _contextMenuView.CloseContextPopups();
            _verbSystem.CloseVerbMenu();
        }

        public void Dispose()
        {
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

            CommandBinds.Unregister<ContextMenuPresenter>();
        }
    }
}
