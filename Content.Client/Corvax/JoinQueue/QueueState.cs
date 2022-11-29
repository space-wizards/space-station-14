using Content.Shared.Corvax.JoinQueue;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.Corvax.JoinQueue;

public sealed class QueueState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

    private const string JoinSoundPath = "/Audio/Effects/voteding.ogg";

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

        Ding();
    }

    private void Ding()
    {
        if (IoCManager.Resolve<IEntityManager>().TrySystem<AudioSystem>(out var audio))
        {
            audio.PlayGlobal(JoinSoundPath, Filter.Local(), false);
        }
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