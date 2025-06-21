using System.Numerics;
using System.Threading;
using Content.Client.CombatMode;
using Content.Client.Gameplay;
using Content.Client.Mapping;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.ContextMenu.UI
{
    /// <summary>
    ///     This class handles all the logic associated with showing a context menu, as well as all the state for the
    ///     entire context menu stack, including verb and entity menus. It does not currently support multiple
    ///     open context menus.
    /// </summary>
    /// <remarks>
    ///     This largely involves setting up timers to open and close sub-menus when hovering over other menu elements.
    /// </remarks>
    public sealed class ContextMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CombatModeSystem>, IOnStateEntered<MappingState>, IOnStateExited<MappingState>
    {
        public static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

        /// <summary>
        ///     Root menu of the entire context menu.
        /// </summary>
        public ContextMenuPopup RootMenu = default!;
        public Stack<ContextMenuPopup> Menus { get; } = new();

        /// <summary>
        ///     Used to cancel the timer that opens menus.
        /// </summary>
        public CancellationTokenSource? CancelOpen;

        /// <summary>
        ///     Used to cancel the timer that closes menus.
        /// </summary>
        public CancellationTokenSource? CancelClose;

        public Action? OnContextClosed;
        public Action<ContextMenuElement>? OnContextMouseEntered;
        public Action<ContextMenuElement>? OnContextMouseExited;
        public Action<ContextMenuElement>? OnSubMenuOpened;
        public Action<ContextMenuElement, GUIBoundKeyEventArgs>? OnContextKeyEvent;

        private bool _setup;

        public void OnStateEntered(GameplayState state)
        {
            Setup();
        }

        public void OnStateExited(GameplayState state)
        {
            Shutdown();
        }

        public void OnStateEntered(MappingState state)
        {
            Setup();
        }

        public void OnStateExited(MappingState state)
        {
            Shutdown();
        }

        public void Setup()
        {
            if (_setup)
                return;

            _setup = true;

            RootMenu = new(this, null);
            RootMenu.OnPopupHide += Close;
            Menus.Push(RootMenu);
        }

        public void Shutdown()
        {
            if (!_setup)
                return;

            _setup = false;

            Close();
            RootMenu.OnPopupHide -= Close;
            RootMenu.Dispose();
            RootMenu = default!;
        }

        /// <summary>
        ///     Close and clear the root menu. This will also dispose any sub-menus.
        /// </summary>
        public void Close()
        {
            RootMenu.MenuBody.DisposeAllChildren();
            CancelOpen?.Cancel();
            CancelClose?.Cancel();
            OnContextClosed?.Invoke();
            RootMenu.Close();
        }

        /// <summary>
        ///     Starts closing menus until the top-most menu is the given one.
        /// </summary>
        /// <remarks>
        ///     Note that this does not actually check if the given menu IS a sub menu of this presenter. In that case
        ///     this will close all menus.
        /// </remarks>
        public void CloseSubMenus(ContextMenuPopup? menu)
        {
            if (menu == null || !menu.Visible)
                return;

            while (Menus.TryPeek(out var subMenu) && subMenu != menu)
            {
                Menus.Pop().Close();
            }

            // ensure no accidental double-closing happens.
            CancelClose?.Cancel();
            CancelClose = null;
        }

        /// <summary>
        ///     Start a timer to open this element's sub-menu.
        /// </summary>
        private void OnMouseEntered(ContextMenuElement element)
        {
            if (!Menus.TryPeek(out var topMenu))
            {
                Log.Error("Context Menu: Mouse entered menu without any open menus?");
                return;
            }

            if (element.ParentMenu == topMenu || element.SubMenu == topMenu)
                CancelClose?.Cancel();

            if (element.SubMenu == topMenu)
                return;

            // open the sub-menu after a short delay.
            CancelOpen?.Cancel();
            CancelOpen = new();
            Timer.Spawn(HoverDelay, () => OpenSubMenu(element), CancelOpen.Token);
        }

        /// <summary>
        ///     Start a timer to close this element's sub-menu.
        /// </summary>
        /// <remarks>
        ///     Note that this timer will be aborted when entering the actual sub-menu itself.
        /// </remarks>
        private void OnMouseExited(ContextMenuElement element)
        {
            CancelOpen?.Cancel();

            if (element.SubMenu == null)
                return;

            CancelClose?.Cancel();
            CancelClose = new();
            Timer.Spawn(HoverDelay, () => CloseSubMenus(element.ParentMenu), CancelClose.Token);
            OnContextMouseExited?.Invoke(element);
        }

        private void OnKeyBindDown(ContextMenuElement element, GUIBoundKeyEventArgs args)
        {
            OnContextKeyEvent?.Invoke(element, args);
        }

        /// <summary>
        ///     Opens a new sub menu, and close the old one.
        /// </summary>
        /// <remarks>
        ///     If the given element has no sub-menu, just close the current one.
        /// </remarks>
        public void OpenSubMenu(ContextMenuElement element)
        {
            if (!Menus.TryPeek(out var topMenu))
            {
                Log.Error("Context Menu: Attempting to open sub menu without any open menus?");
                return;
            }

            // If This is already the top most menu, do nothing.
            if (element.SubMenu == topMenu)
                return;

            // Was the parent menu closed or disposed before an open timer completed?
            if (element.Disposed || element.ParentMenu == null || !element.ParentMenu.Visible)
                return;

            // Close any currently open sub-menus up to this element's parent menu.
            CloseSubMenus(element.ParentMenu);

            // cancel any queued openings to prevent weird double-open scenarios.
            CancelOpen?.Cancel();
            CancelOpen = null;

            if (element.SubMenu == null)
                return;

            // open pop-up adjacent to the parent element. We want the sub-menu elements to align with this element
            // which depends on the panel container style margins.
            var altPos = element.GlobalPosition;
            var pos = altPos + new Vector2(element.Width + 2 * ContextMenuElement.ElementMargin, -2 * ContextMenuElement.ElementMargin);
            element.SubMenu.Open(UIBox2.FromDimensions(pos, new Vector2(1, 1)), altPos);

            // draw on top of other menus
            element.SubMenu.SetPositionLast();

            Menus.Push(element.SubMenu);
            OnSubMenuOpened?.Invoke(element);
        }

        /// <summary>
        ///     Add an element to a menu and subscribe to GUI events.
        /// </summary>
        public void AddElement(ContextMenuPopup menu, ContextMenuElement element)
        {
            element.OnMouseEntered += _ => OnMouseEntered(element);
            element.OnMouseExited += _ => OnMouseExited(element);
            element.OnKeyBindDown += args => OnKeyBindDown(element, args);
            element.ParentMenu = menu;
            menu.MenuBody.AddChild(element);
            menu.InvalidateMeasure();
        }

        /// <summary>
        ///     Removes event subscriptions when an element is removed from a menu,
        /// </summary>
        public void OnRemoveElement(ContextMenuPopup menu, Control control)
        {
            if (control is not ContextMenuElement element)
                return;

            element.OnMouseEntered -= _ => OnMouseEntered(element);
            element.OnMouseExited -= _ => OnMouseExited(element);
            element.OnKeyBindDown -= args => OnKeyBindDown(element, args);

            menu.InvalidateMeasure();
        }

        private void OnCombatModeUpdated(bool inCombatMode)
        {
            if (inCombatMode)
                Close();
        }

        public void OnSystemLoaded(CombatModeSystem system)
        {
            system.LocalPlayerCombatModeUpdated += OnCombatModeUpdated;
        }

        public void OnSystemUnloaded(CombatModeSystem system)
        {
            system.LocalPlayerCombatModeUpdated -= OnCombatModeUpdated;
        }
    }
}
