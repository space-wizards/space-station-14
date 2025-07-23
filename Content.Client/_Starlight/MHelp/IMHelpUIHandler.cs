using Content.Shared.Starlight.MHelp;
using Content.Shared.Administration;
using Robust.Shared.Network;

namespace Content.Client.UserInterface.Systems.Bwoink;

// please kill all this indirection
public interface IMHelpUIHandler : IDisposable
{
    public bool IsMentor { get; }
    public bool IsOpen { get; }
    public void Receive(SharedMentorSystem.MHelpTextMessage message);
    public void Close();
    public void Open(NetUserId netUserId);
    public void ToggleWindow();
    public void PeopleTypingUpdated(MHelpTypingUpdated args);
    public event Action OnClose;
    public event Action OnOpen;
    public event Action<Guid?, string, bool> OnMessageSend;
    public event Action<Guid?, string> OnInputTextChanged;
    public event Action<Guid> OnTicketClosed;
    public event Action<Guid> OnTptoPressed;
}
