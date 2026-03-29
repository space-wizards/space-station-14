namespace Content.Shared.Localizations.Context;

public record UserTarget(EntityUid User, EntityUid Target) : IContext
{
    public string GetString(ILocalizationManager loc, string messageId)
        => loc.GetString(messageId, ("user", User), ("target", Target));
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0)
        => loc.GetString(messageId, ("user", User), ("target", Target), arg0);
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0, (string, object) arg1)
        => loc.GetString(messageId, ("user", User), ("target", Target), arg0, arg1);
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => loc.GetString(messageId, ("user", User), ("target", Target), arg0, arg1, arg2);
}

