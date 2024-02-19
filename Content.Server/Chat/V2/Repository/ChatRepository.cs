namespace Content.Server.Chat.V2.Repository;

public sealed class ChatRepository
{
    private uint _clock = 0;
    private Dictionary<uint, EntityEventArgs> _messages = new Dictionary<uint, EntityEventArgs>();
    private Dictionary<string, HashSet<uint>> _playerMessages = new Dictionary<string, HashSet<uint>>();
}
