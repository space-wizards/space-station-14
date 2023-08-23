// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Messenger;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges.Messenger;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiState? _messengerUiState;
    private string? _errorText;

    private const int ChatsList = 0;
    private const int ChatHistory = 1;

    private int _currentView;

    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _currentView = ChatsList;
        _fragment = new MessengerUiFragment();
        _fragment.ChatsSearch.OnTextChanged += args =>
        {
            _fragment.SearchString = args.Text == "" ? null : args.Text.ToLower();
            UpdateUiState();
        };

        _fragment.OnHistoryViewPressed += chatId =>
        {
            _fragment.CurrentChat = chatId;
            _currentView = ChatHistory;
            _fragment.SearchString = null;

            UpdateUiState();
            ValidateState(userInterface);
        };

        _fragment.OnMessageSendButtonPressed += (chatId, text) =>
        {
            var message = new MessengerSendMessageUiEvent(chatId, text);
            var ms = new CartridgeUiMessage(message);
            userInterface.SendMessage(ms);
        };

        _fragment.OnBackButtonPressed += _ =>
        {
            _fragment.SearchString = null;

            if (_currentView != ChatsList)
            {
                _currentView--;
            }

            switch (_currentView)
            {
                case ChatsList:
                {
                    if (_messengerUiState != null)
                        _fragment?.UpdateChatsState(_messengerUiState);
                    break;
                }
            }
        };

        if (_messengerUiState == null)
        {
            var message = new MessengerUpdateStateUiEvent(true);
            var ms = new CartridgeUiMessage(message);
            userInterface.SendMessage(ms);
            return;
        }

        UpdateUiState();
        ValidateState(userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        _errorText = null;

        switch (state)
        {
            case MessengerUiState messengerUiState:
            {
                _messengerUiState = messengerUiState;
                break;
            }
            case MessengerClientContactUiState contactUiState:
            {
                if (_messengerUiState == null)
                    break;

                _messengerUiState.ClientContact = contactUiState.ClientContact;
                break;
            }
            case MessengerContactUiState messengerContactUiState:
            {
                if (_messengerUiState == null)
                    break;

                foreach (var messengerContact in messengerContactUiState.Contacts)
                {
                    _messengerUiState.Contacts[messengerContact.Id] = messengerContact;
                }

                break;
            }
            case MessengerMessagesUiState messengerMessagesUiState:
            {
                if (_messengerUiState == null)
                    break;

                foreach (var messengerMessage in messengerMessagesUiState.Messages)
                {
                    _messengerUiState.Messages[messengerMessage.Id] = messengerMessage;

                    var chat = GetOrCreateChat(messengerMessage.ChatId);

                    chat.Messages.Add(messengerMessage.Id);
                    chat.LastMessage = chat.Messages.Max();
                }

                break;
            }
            case MessengerNewChatMessageUiState st:
            {
                if (_messengerUiState == null)
                    break;

                _messengerUiState.Messages.TryAdd(st.Message.Id, st.Message);

                var chat = GetOrCreateChat(st.ChatId);

                chat.Messages.Add(st.Message.Id);
                chat.LastMessage = st.Message.Id;

                break;
            }
            case MessengerChatUpdateUiState st:
            {
                if (_messengerUiState == null)
                    break;

                foreach (var messengerChat in st.Chats)
                {
                    if (_messengerUiState.Chats.TryGetValue(messengerChat.Id, out var chatUi))
                    {
                        chatUi.Name = messengerChat.Name ?? chatUi.Name;
                        chatUi.Members.UnionWith(messengerChat.MembersId);
                        chatUi.LastMessage = messengerChat.LastMessageId ?? chatUi.LastMessage;
                        chatUi.Messages.UnionWith(messengerChat.MessagesId);
                        chatUi.NewMessages = true;
                        break;
                    }

                    _messengerUiState.Chats.Add(messengerChat.Id,
                        new MessengerChatUiState(messengerChat.Id, messengerChat.Name, messengerChat.Kind,
                            messengerChat.MembersId, messengerChat.MessagesId, messengerChat.LastMessageId,
                            _messengerUiState.Chats.Count));
                }

                break;
            }
            case MessengerErrorUiState errorUiState:
            {
                _errorText = errorUiState.Text;
                break;
            }
        }

        UpdateUiState();
    }

    private MessengerChatUiState GetOrCreateChat(uint chatId)
    {
        if (_messengerUiState == null)
        {
            return new MessengerChatUiState(chatId, null, MessengerChatKind.Contact, new HashSet<uint>(),
                new HashSet<uint>(), null, 0);
        }

        if (!_messengerUiState.Chats.TryGetValue(chatId, out var chat))
        {
            chat = new MessengerChatUiState(chatId, null, MessengerChatKind.Contact, new HashSet<uint>(),
                new HashSet<uint>(), null, _messengerUiState.Chats.Count);

            chat.ForceUpdate = true;

            _messengerUiState.Chats.Add(chatId, chat);

            return chat;
        }

        return chat;
    }

    private void ValidateState(BoundUserInterface userInterface)
    {
        var receivedContacts = new HashSet<uint>();
        var receivedMessages = new HashSet<uint>();
        var receivedChats = new HashSet<uint>();

        if (_messengerUiState == null)
            return;

        foreach (var (chatId, chat) in _messengerUiState.Chats)
        {
            if (!chat.ForceUpdate)
            {
                receivedChats.Add(chatId);
            }

            chat.ForceUpdate = false;
        }

        foreach (var (contactId, _) in _messengerUiState.Contacts)
        {
            receivedContacts.Add(contactId);
        }

        foreach (var (messageId, _) in _messengerUiState.Messages)
        {
            receivedMessages.Add(messageId);
        }

        userInterface.SendMessage(
            new CartridgeUiMessage(new MessengerUpdateStateUiEvent(receivedContacts, receivedMessages, receivedChats)));
    }

    private void UpdateUiState()
    {
        switch (_currentView)
        {
            case ChatsList:
            {
                if (_messengerUiState != null)
                    _fragment?.UpdateChatsState(_messengerUiState);
                break;
            }
            case ChatHistory:
            {
                if (_messengerUiState != null)
                    _fragment?.UpdateChatHistoryState(_messengerUiState);
                break;
            }
        }

        if (_errorText != null)
            _fragment?.DisplayError(_errorText);
    }
}
