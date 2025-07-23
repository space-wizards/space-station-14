using System.Numerics;
using Content.Client._Starlight.MHelp.UI;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Starlight.MHelp;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.MHelp;

public sealed class UserMHelpUIHandler(NetUserId owner) : IMHelpUIHandler
{
    public bool IsMentor => false;
    public bool IsOpen => _window is { Disposed: false, IsOpen: true };
    private DefaultWindow? _window;
    private MhelpPanel? _chatPanel;
    private Guid? _currentTicket;

    public void Receive(SharedMentorSystem.MHelpTextMessage message)
    {
        EnsureInit();
        _currentTicket = message.TicketClosed ? null : message.Ticket;
        _chatPanel!.ReceiveLine(message);
        _window!.OpenCentered();
    }

    public void Close() => _window?.Close();

    public void ToggleWindow()
    {
        EnsureInit();
        if (_window!.IsOpen)
            _window.Close();
        else
            _window.OpenCentered();
    }

    public void PeopleTypingUpdated(MHelpTypingUpdated args)
    {
    }

    public event Action OnClose = delegate { };
    public event Action OnOpen = delegate { };
    public event Action<Guid?, string, bool> OnMessageSend = delegate { };
    public event Action<Guid?, string> OnInputTextChanged = delegate { };
    public event Action<Guid> OnTicketClosed = delegate { };
    public event Action<Guid> OnTptoPressed = delegate { };

    public void Open(NetUserId channelId)
    {
        EnsureInit();
        _window!.OpenCentered();
    }

    private void EnsureInit()
    {
        if (_window is { Disposed: false })
            return;
        _chatPanel = new MhelpPanel();
        _chatPanel.OnMessageSend += text => OnMessageSend.Invoke(_currentTicket, text, true);
        _chatPanel.OnInputTextChanged += text => OnInputTextChanged.Invoke(_currentTicket, text);
        _chatPanel.OnTicketClosed += () =>
        {
            if (_currentTicket.HasValue)
                OnTicketClosed.Invoke(_currentTicket.Value);
        };
        //In-theory it shouldn't be possible to call this
        //But debugging.
        _chatPanel.OnTptoPressed += () =>
        {
            if (_currentTicket.HasValue)
                OnTptoPressed.Invoke(_currentTicket.Value);
        };
        _window = new DefaultWindow()
        {
            TitleClass = "windowTitleAlert",
            HeaderClass = "windowHeaderCyanAlert",
            Title = Loc.GetString("mentor-user-title"),
            MinSize = new Vector2(500, 300),
        };
        _window.OnClose += () => OnClose?.Invoke();
        _window.OnOpen += () => OnOpen?.Invoke();
        _window.Contents.AddChild(_chatPanel);
        _chatPanel.ShowTpto = IsMentor;
    }

    public void Dispose()
    {
        _window = null;
        _chatPanel = null;
    }
}
