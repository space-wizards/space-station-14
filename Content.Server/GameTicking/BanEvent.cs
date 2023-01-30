namespace Content.Shared.GameTicking;

public sealed class BanEvent : EntityEventArgs
{
    public string Username { get; }
    public DateTimeOffset? Expires { get; }
    public string Reason { get; }


    public BanEvent(string username, DateTimeOffset? expires, string reason)
    {
        Username = username;
        Expires = expires;
        Reason = reason;
    }
}
