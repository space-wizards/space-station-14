// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Messenger;

namespace Content.Server.Messenger;

[RegisterComponent]
public sealed partial class MessengerServerComponent : Component
{
    [DataField(("serverName"))] public string Name = "";

    // store EntityUid as authentication entity to contactKey
    private readonly Dictionary<EntityUid, ContactKey> _clientToContact = new();

    // store contact info by contactKey
    private readonly SequenceDataStore<ContactKey, MessengerContact> _contactsStore = new();

    // store chat info by chat key
    private readonly SequenceDataStore<ChatKey, MessengerChat> _chatsStore = new();

    // store message info by message key
    private readonly SequenceDataStore<MessageKey, MessengerMessage> _messagesStore = new();

    // store chats which can find any contact connected to server
    private readonly HashSet<ChatKey> _publicChats = new();

    // store chats which can find only invited contact, like private chats
    private readonly SequenceDataStore<ContactKey, HashSet<ChatKey>> _privateChats = new();

    public List<KeyValuePair<EntityUid, ContactKey>> GetClientToContact()
    {
        return new List<KeyValuePair<EntityUid, ContactKey>>(_clientToContact);
    }

    public bool GetContactKey(EntityUid client, [NotNullWhen(true)] out ContactKey? key)
    {
        key = null;

        if (!_clientToContact.ContainsKey(client))
            return false;

        key = _clientToContact[client];

        return true;
    }

    public MessengerContact GetContact(ContactKey key)
    {
        var messengerContact = _contactsStore.Get(key);
        if (messengerContact == null)
            return new();
        messengerContact.Id = key.Id;
        return messengerContact;
    }

    public HashSet<ChatKey> GetPrivateChats(ContactKey key)
    {
        var chats = _privateChats.Get(key);

        if (chats == null)
            return new();

        return new HashSet<ChatKey>(chats);
    }

    public HashSet<ChatKey> GetPublicChats()
    {
        return new HashSet<ChatKey>(_publicChats);
    }

    public ContactKey AddEntityContact(EntityUid uid, string name, string netAddress)
    {
        if (_clientToContact.TryGetValue(uid, out var contact))
            return contact;

        var contactKey = _contactsStore.Add(new MessengerContact(name, netAddress));
        _clientToContact.Add(uid, contactKey);

        return contactKey;
    }

    public void UpdateContactName(ContactKey key, string? name)
    {
        if (name == null)
            return;

        var contact = _contactsStore.Get(key);

        if (contact == null)
            return;

        if (contact.Name == name)
            return;

        contact.Name = name;
    }

    public ChatKey AddChat(MessengerChat chat)
    {
        return _chatsStore.Add(chat);
    }

    public MessengerChat GetChat(ChatKey key)
    {
        var chat = _chatsStore.Get(key);
        if (chat == null)
            return new();
        chat.Id = key.Id;
        return chat;
    }

    public MessageKey AddMessage(MessengerMessage message)
    {
        return _messagesStore.Add(message);
    }

    public MessengerMessage GetMessage(MessageKey key)
    {
        var message = _messagesStore.Get(key);
        if (message == null)
            return new();
        message.Id = key.Id;
        return message;
    }

    public void AddPublicChat(ChatKey chatKey)
    {
        _publicChats.Add(chatKey);
    }

    public void AddPrivateChats(ContactKey contact, List<ChatKey> chats)
    {
        AddKeyToHasSet(_privateChats, contact, chats);
    }

    public void AddPrivateChats(ContactKey contact, ChatKey chat)
    {
        AddPrivateChats(contact, new List<ChatKey> { chat });
    }

    private void AddKeyToHasSet<TKey, TValue>(SequenceDataStore<TKey, HashSet<TValue>> storage, TKey key,
        List<TValue> list) where TKey : IId, new()
    {
        var existList = storage.Get(key);

        if (existList == null)
        {
            storage.Set(key, new HashSet<TValue>(list));
            return;
        }

        existList.UnionWith(list);

        storage.Set(key, existList);
    }
}

public sealed class ContactKey : Key
{
    public ContactKey() { }
    public ContactKey(uint id) : base(id) { }
}

public sealed class MessageKey : Key
{
    public MessageKey() { }
    public MessageKey(uint id) : base(id) { }
}

public sealed class ChatKey : Key
{
    public ChatKey() { }
    public ChatKey(uint id) : base(id) { }
}

public abstract class Key : IId
{
    public uint Id { get; init; }

    protected Key()
    {
        Id = 0;
    }

    protected Key(uint id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return (int) Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Key && Equals((Key) obj);
    }

    private bool Equals(Key p)
    {
        return Id == p.Id;
    }
}

public sealed class SequenceDataStore<TKey, TValue> where TKey : IId, new() where TValue : notnull, new()
{
    private uint _sequence;
    private readonly Dictionary<uint, TValue> _storage = new();

    public TKey Add(TValue value)
    {
        if (!_storage.TryAdd(_sequence, value))
        {
            _sequence++;
            Add(value);
        }

        var t = new TKey
        {
            Id = _sequence
        };

        _sequence++;
        return t;
    }

    public void Set(TKey key, TValue value)
    {
        _storage[key.Id] = value;
    }

    public TValue? Get(TKey key)
    {
        if (!_storage.ContainsKey(key.Id))
            return default;

        return _storage[key.Id];
    }

    public bool Delete(TKey key)
    {
        return _storage.Remove(key.Id);
    }
}

public interface IId
{
    public uint Id { get; init; }
}
