namespace Content.Shared.GameTicking;

public sealed class BanEvent : EntityEventArgs
{
    public string? AdminName { get; }
    public string Username { get; }
    public DateTimeOffset? Expires { get; }
    public string Reason { get; }
    public BanEvent(string username, DateTimeOffset? expires, string reason, string? adminname = null)
    {
        AdminName = adminname;
        Username = username;
        Expires = expires;
        Reason = reason;
    }
}
