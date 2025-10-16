﻿using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.Tabs.ObjectsTab;
using Content.Client.Administration.UI.Tabs.PanicBunkerTab;
using Content.Client.Administration.UI.Tabs.PlayerTab;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.Administration.Events;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Admin;

[UsedImplicitly]
public sealed class AdminUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateEntered<LobbyState>,
    IOnSystemChanged<AdminSystem>
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IClientConGroupController _conGroups = default!;
    [Dependency] private readonly IClientConsoleHost _conHost = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly VerbMenuUIController _verb = default!;

    private AdminMenuWindow? _window;
    private MenuButton? AdminButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.AdminButton;
    private PanicBunkerStatus? _panicBunker;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PanicBunkerChangedEvent>(OnPanicBunkerUpdated);
    }

    private void OnPanicBunkerUpdated(PanicBunkerChangedEvent msg, EntitySessionEventArgs args)
    {
        var showDialog = _panicBunker == null && msg.Status.Enabled;
        _panicBunker = msg.Status;
        _window?.PanicBunkerControl.UpdateStatus(msg.Status);

        if (showDialog)
        {
            UIManager.CreateWindow<PanicBunkerStatusWindow>().OpenCentered();
        }
    }

    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();
        AdminStatusUpdated();
    }

    public void OnStateEntered(LobbyState state)
    {
        EnsureWindow();
        AdminStatusUpdated();
    }

    public void OnSystemLoaded(AdminSystem system)
    {
        EnsureWindow();

        _admin.AdminStatusUpdated += AdminStatusUpdated;
        _input.SetInputCommand(ContentKeyFunctions.OpenAdminMenu,
            InputCmdHandler.FromDelegate(_ => Toggle()));
    }

    public void OnSystemUnloaded(AdminSystem system)
    {
        if (_window != null)
            _window.Dispose();

        _admin.AdminStatusUpdated -= AdminStatusUpdated;

        CommandBinds.Unregister<AdminUIController>();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        if (_window?.Disposed ?? false)
            OnWindowDisposed();

        _window = UIManager.CreateWindow<AdminMenuWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);

        if (_panicBunker != null)
            _window.PanicBunkerControl.UpdateStatus(_panicBunker);

        _window.PlayerTabControl.OnEntryKeyBindDown += PlayerTabEntryKeyBindDown;
        _window.ObjectsTabControl.OnEntryKeyBindDown += ObjectsTabEntryKeyBindDown;
        _window.OnOpen += OnWindowOpen;
        _window.OnClose += OnWindowClosed;
        _window.OnDisposed += OnWindowDisposed;
    }

    public void UnloadButton()
    {
        if (AdminButton == null)
        {
            return;
        }

        AdminButton.OnPressed -= AdminButtonPressed;
    }

    public void LoadButton()
    {
        if (AdminButton == null)
        {
            return;
        }

        AdminButton.OnPressed += AdminButtonPressed;
    }

    private void OnWindowOpen()
    {
        AdminButton?.SetClickPressed(true);
    }

    private void OnWindowClosed()
    {
        AdminButton?.SetClickPressed(false);
    }

    private void OnWindowDisposed()
    {
        if (AdminButton != null)
            AdminButton.Pressed = false;

        if (_window == null)
            return;

        _window.PlayerTabControl.OnEntryKeyBindDown -= PlayerTabEntryKeyBindDown;
        _window.ObjectsTabControl.OnEntryKeyBindDown -= ObjectsTabEntryKeyBindDown;
        _window.OnOpen -= OnWindowOpen;
        _window.OnClose -= OnWindowClosed;
        _window.OnDisposed -= OnWindowDisposed;
        _window = null;
    }

    private void AdminStatusUpdated()
    {
        if (AdminButton != null)
            AdminButton.Visible = _conGroups.CanAdminMenu();
    }

    private void AdminButtonPressed(ButtonEventArgs args)
    {
        Toggle();
    }

    private void Toggle()
    {
        if (_window is {IsOpen: true})
        {
            _window.Close();
        }
        else if (_conGroups.CanAdminMenu())
        {
            _window?.Open();
        }
    }

    private void PlayerTabEntryKeyBindDown(GUIBoundKeyEventArgs args, ListData? data)
    {
        if (data is not PlayerListData {Info: var info})
            return;

        if (info.NetEntity == null)
            return;

        var entity = info.NetEntity.Value;
        var function = args.Function;

        if (function == EngineKeyFunctions.UIClick)
            _conHost.ExecuteCommand($"vv {entity}");
        else if (function == EngineKeyFunctions.UIRightClick)
            _verb.OpenVerbMenu(entity, true);
        else
            return;

        args.Handle();
    }

    private void ObjectsTabEntryKeyBindDown(GUIBoundKeyEventArgs args, ListData? data)
    {
        if (data is not ObjectsListData { Info: var info })
            return;

        var uid = info.Entity;
        var function = args.Function;

        if (function == EngineKeyFunctions.UIClick)
            _conHost.ExecuteCommand($"vv {uid}");
        else if (function == EngineKeyFunctions.UIRightClick)
            _verb.OpenVerbMenu(uid, true);
        else
            return;

        args.Handle();
    }
}
