using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.BugReport.Windows;
using Content.Client.UserInterface.Systems.Character;
using Content.Shared.BugReport;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;

namespace Content.Client.UserInterface.Systems.BugReport;

[UsedImplicitly]
public sealed class BugReportUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientNetManager _net = default!;

    // This is the link to the hotbar button
    private MenuButton? BugReportButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.ReportBugButton;

    // Don't clear this window. It needs to be saved so the input doesn't get erased when it's closed!
    private BugReportWindow _bugReportWindow = default!;

    public void OnStateEntered(GameplayState state)
    {
        _bugReportWindow = UIManager.CreateWindow<BugReportWindow>();

        SetupWindow();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.BugReportMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<BugReportUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _bugReportWindow.Close();

        CommandBinds.Unregister<BugReportUIController>();
    }

    public override void Initialize(){}

    private void SetupWindow()
    {
        _bugReportWindow = UIManager.CreateWindow<BugReportWindow>();
        // This is to make sure the hotbar button gets checked and unchecked when the window is opened / closed.
        _bugReportWindow.OnClose += () => BugReportButton!.Pressed = false;
        _bugReportWindow.OnOpen += () => BugReportButton!.Pressed = true;

        _bugReportWindow.OnBugReportSubmitted += OnBugReportSubmitted;
    }

    private void ToggleWindow()
    {
        if (_bugReportWindow.IsOpen)
        {
            _bugReportWindow.Close();

            return;
        }

        _bugReportWindow.OpenCentered();
    }

    private void OnBugReportSubmitted(PlayerBugReportInformation report)
    {
        _net.ClientSendMessage(new BugReportMessage{ ReportInformation = report });
        _bugReportWindow.Close();
    }

    public void LoadButton()
    {
        if (BugReportButton == null)
            return;

        BugReportButton.OnPressed += ButtonToggleWindow;
    }
    public void UnloadButton()
    {
        if (BugReportButton == null)
            return;

        BugReportButton.OnPressed -= ButtonToggleWindow;
    }

    private void ButtonToggleWindow(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }
}
