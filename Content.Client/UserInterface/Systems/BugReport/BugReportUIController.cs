using Content.Client.Gameplay;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.BugReport.Windows;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.BugReport;
using Content.Shared.CCVar;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.BugReport;

[UsedImplicitly]
public sealed class BugReportUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resource = default!;

    // This is the link to the hotbar button
    private MenuButton BugReportButton => UIManager.GetActiveUIWidget<GameTopMenuBar>().ReportBugButton;

    // Don't clear this window. It needs to be saved so the input doesn't get erased when it's closed!
    private BugReportWindow _bugReportWindow = default!;

    private ResPath Bug = new("/Textures/Interface/bug.svg.192dpi.png");
    private ResPath Splat = new("/Textures/Interface/splat.svg.192dpi.png");

    public void OnStateEntered(GameplayState state)
    {
        _bugReportWindow = UIManager.CreateWindow<BugReportWindow>();

        SetupWindow();
    }

    public void OnStateExited(GameplayState state)
    {
        _bugReportWindow.Close();

        CommandBinds.Unregister<BugReportUIController>();
    }

    public void LoadButton()
    {
        BugReportButton.OnPressed += ButtonToggleWindow;
    }

    public void UnloadButton()
    {
        BugReportButton.OnPressed -= ButtonToggleWindow;
    }

    private void SetupWindow()
    {
        _bugReportWindow = UIManager.CreateWindow<BugReportWindow>();
        // This is to make sure the hotbar button gets checked and unchecked when the window is opened / closed.
        _bugReportWindow.OnClose += () =>
        {
            BugReportButton.Pressed = false;
            BugReportButton.Icon = _resource.GetTexture(Bug);
        };
        _bugReportWindow.OnOpen += () =>
        {
            BugReportButton.Pressed = true;
            BugReportButton.Icon = _resource.GetTexture(Splat);
        };

        _bugReportWindow.OnBugReportSubmitted += OnBugReportSubmitted;

        _cfg.OnValueChanged(CCVars.EnablePlayerBugReports, x => BugReportButton.Visible = x, true);
    }

    private void ToggleWindow()
    {
        if (_bugReportWindow.IsOpen)
            _bugReportWindow.Close();
        else
            _bugReportWindow.OpenCentered();
    }

    private void OnBugReportSubmitted(PlayerBugReportInformation report)
    {
        var message = new BugReportMessage { ReportInformation = report };
        _net.ClientSendMessage(message);
        _bugReportWindow.Close();
    }

    private void ButtonToggleWindow(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }
}
