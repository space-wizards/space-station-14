using Content.Shared.JoinQueue;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client.JoinQueue;

public sealed class QueueState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    
    private QueueGui? _gui;
    
    protected override void Startup()
    {
        _gui = new QueueGui();
        _userInterfaceManager.StateRoot.AddChild(_gui);
        
        _gui.QuitPressed += OnQuitPressed;
    }

    protected override void Shutdown()
    {
        _gui!.QuitPressed -= OnQuitPressed;
        _gui.Dispose();
    }
    
    public void OnQueueUpdate(MsgQueueUpdate msg)
    {
        _gui?.UpdateInfo(msg.Total, msg.Position);
    }
    
    private void OnQuitPressed()
    {
        _consoleHost.ExecuteCommand("quit");
    }
}