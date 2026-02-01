namespace Content.Shared.Localizations;

/// <summary>
/// Static wrapper for the <see cref="ILocalizationManager"/>, that provides
/// general use parameters for Localization use.
/// </summary>
/// <seealso cref="Robust.Shared.Localization.Loc"/>
public static partial class ContextedLoc
{
    private static ILocalizationManager _loc => IoCManager.Resolve<ILocalizationManager>();

    /// <summary>
    ///     Gets a language appropriate string represented by the supplied messageId.
    /// </summary>
    /// <param name="messageId">Unique Identifier for a translated message.</param>
    /// <param name="context">Context to pass into translation system</param>
    /// <returns>
    ///     The language appropriate message if available, otherwise the messageId is returned.
    /// </returns>
    public static string GetString(string messageId, IContext context)
        => context.GetString(_loc, messageId);

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, IContext context, (string, object) arg0)
        => context.GetString(_loc, messageId, arg0);

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, IContext context, (string, object) arg0, (string, object) arg1)
        => context.GetString(_loc, messageId, arg0, arg1);

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, IContext context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => context.GetString(_loc, messageId, arg0, arg1, arg2);
}
