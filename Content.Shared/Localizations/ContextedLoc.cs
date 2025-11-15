namespace Content.Shared.Localizations;

/// <summary>
/// Static wrapper for the <see cref="ILocalizationManager"/>, that provides
/// general use parameters for Localization use.
/// </summary>
/// <seealso cref="Robust.Shared.Localization.Loc"/>
public static partial class ContextedLoc
{
    private static ILocalizationManager _loc => IoCManager.Resolve<ILocalizationManager>();
    private static EntityManager _entity => IoCManager.Resolve<EntityManager>();

    /// <summary>
    ///     Gets a language appropriate string represented by the supplied messageId.
    /// </summary>
    /// <param name="messageId">Unique Identifier for a translated message.</param>
    /// <param name="context">Context to pass into translation system</param>
    /// <returns>
    ///     The language appropriate message if available, otherwise the messageId is returned.
    /// </returns>
    public static string GetString(string messageId, object context)
        => throw new NotImplementedException();

    /// <summary>
    ///     Gets a language appropriate string represented by the supplied messageId.
    /// </summary>
    /// <param name="messageId">Unique Identifier for a translated message.</param>
    /// <param name="entity">Entity that cause message to appear, entity system's ent.Owner should be passed here</param>
    /// <param name="context">Context to pass into translation system</param>
    /// <returns>
    ///     The language appropriate message if available, otherwise the messageId is returned.
    /// </returns>
    public static string GetStringWithEntity(string messageId, EntityUid entity, object context)
        => throw new NotImplementedException();

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, object context, (string, object) arg0)
        => throw new NotImplementedException();
    /// <inheritdoc cref="GetStringWithEntity(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, object context, (string, object) arg0)
        => throw new NotImplementedException();

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, object context, (string, object) arg0, (string, object) arg1)
        => throw new NotImplementedException();
    /// <inheritdoc cref="GetStringWithEntity(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, object context, (string, object) arg0, (string, object) arg1)
        => throw new NotImplementedException();

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, object context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => throw new NotImplementedException();
    /// <inheritdoc cref="GetStringWithEntity(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, object context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => throw new NotImplementedException();
}
