using System.Diagnostics.CodeAnalysis;
using Content.Client._Starlight.MHelp.UI;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Starlight.MHelp;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;

namespace Content.Client._Starlight.MHelp;

public sealed class MentorMHelpUIHandler(NetUserId owner) : IMHelpUIHandler
{
    private readonly Dictionary<Guid, MhelpPanel> _ticketsPanelMap = [];
    public bool IsMentor => true;
    public bool IsOpen => Window is { Disposed: false, IsOpen: true } || ClydeWindow is { IsDisposed: false };
    public bool EverOpened;

    public MentorWindow? Window;
    public WindowRoot? WindowRoot;
    public IClydeWindow? ClydeWindow;
    public MhelpControl? Control;

    public event Action OnClose = delegate { };
    public event Action OnOpen = delegate { };
    public event Action<Guid?, string> OnInputTextChanged = delegate { };
    public event Action<Guid?, string, bool> OnMessageSend = delegate { };
    public event Action<Guid> OnTicketClosed = delegate { };
    public event Action<Guid> OnTptoPressed = delegate { };

    public void Receive(SharedMentorSystem.MHelpTextMessage message)
    {
        if (message.Ticket is null)
            return;
        var panel = EnsurePanel(message.Ticket.Value);
        panel.ReceiveLine(message);
        Control?.EnsureTicket(message.Ticket.Value, message.Title, message.TicketClosed);
        Control?.UpdateTicketList();
    }

    private void OpenWindow()
    {
        if (Window == null)
            return;

        if (EverOpened)
            Window.Open();
        else
            Window.OpenCentered();
    }

    public void Close()
    {
        Window?.Close();

        // popped-out window is being closed
        if (ClydeWindow != null)
        {
            ClydeWindow.RequestClosed -= OnRequestClosed;
            ClydeWindow.Dispose();
            // need to dispose control cause we cant reattach it directly back to the window
            // but orphan panels first so -they- can get readded when the window is opened again
            if (Control != null)
                foreach (var (_, panel) in _ticketsPanelMap)
                    panel.Orphan();
            // window wont be closed here so we will invoke ourselves
            OnClose?.Invoke();
        }
    }

    public void ToggleWindow()
    {
        EnsurePanel(owner);

        if (IsOpen)
            Close();
        else
            OpenWindow();
    }

    public void DiscordRelayChanged(bool active)
    {
    }

    public void PeopleTypingUpdated(MHelpTypingUpdated args)
    {
        if (_ticketsPanelMap.TryGetValue(args.Ticket, out var panel))
            panel.UpdatePlayerTyping(args.PlayerName, args.Typing);
    }

    public void Open(NetUserId channelId)
    {
        SelectChannel(channelId);
        OpenWindow();
    }

    public void OnRequestClosed(WindowRequestClosedEventArgs args)
    {
        Close();
    }

    private void EnsureControl()
    {
        if (Control is { Disposed: false })
            return;

        Window = new MentorWindow();
        Control = Window.MHelpControl;
        Window.OnClose += () => OnClose?.Invoke();
        Window.OnOpen += () =>
        {
            OnOpen?.Invoke();
            EverOpened = true;
        };

        // need to readd any unattached panels..
        foreach (var (_, panel) in _ticketsPanelMap)
        {
            if (!Control!.MhelpArea.Children.Contains(panel))
                Control!.MhelpArea.AddChild(panel);
            panel.Visible = false;
        }
    }

    public void HideAllPanels()
    {
        foreach (var panel in _ticketsPanelMap.Values)
            panel.Visible = false;
    }

    public MhelpPanel EnsurePanel(Guid ticketId, bool closed = false)
    {
        EnsureControl();

        if (_ticketsPanelMap.TryGetValue(ticketId, out var existingPanel))
        {
            existingPanel.SetInputVisibility(!closed);
            return existingPanel;
        }

        _ticketsPanelMap[ticketId] = existingPanel = new MhelpPanel();
        existingPanel.OnMessageSend += text => OnMessageSend?.Invoke(ticketId, text, Window?.MHelpControl.PlaySound.Pressed ?? true);
        existingPanel.OnInputTextChanged += text => OnInputTextChanged?.Invoke(ticketId, text);
        existingPanel.OnTicketClosed += () => OnTicketClosed.Invoke(ticketId);
        existingPanel.OnTptoPressed += () => OnTptoPressed.Invoke(ticketId);
        existingPanel.ShowTpto = IsMentor;
        existingPanel.Visible = false;
        if (!Control!.MhelpArea.Children.Contains(existingPanel))
            Control.MhelpArea.AddChild(existingPanel);

        return existingPanel;
    }
    public bool TryGetChannel(Guid ticket, [NotNullWhen(true)] out MhelpPanel? panel) => _ticketsPanelMap.TryGetValue(ticket, out panel);

    private void SelectChannel(Guid ticket)
    {
        EnsurePanel(ticket);
        Control!.SelectTicket(ticket);
    }

    public void Dispose()
    {
        Window = null;
        Control = null;
        _ticketsPanelMap.Clear();
        EverOpened = false;
    }

    internal void SendCloseTicket(Guid ticketIid) => OnTicketClosed.Invoke(ticketIid);
}
