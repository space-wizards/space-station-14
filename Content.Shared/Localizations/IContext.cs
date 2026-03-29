namespace Content.Shared.Localizations;

public interface IContext
{
    public string GetString(ILocalizationManager loc, string messageId);
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0);
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0, (string, object) arg1);
    public string GetString(ILocalizationManager loc, string messageId, (string, object) arg0, (string, object) arg1, (string, object) arg2);
}
