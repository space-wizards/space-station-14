using Content.Shared.Interaction;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, InteractHandEvent context)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)));
    /// <inheritdoc cref="GetString(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, InteractHandEvent context)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)));

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, InteractHandEvent context, (string, object) arg0)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0);
    /// <inheritdoc cref="GetString(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, InteractHandEvent context, (string, object) arg0)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0);

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, InteractHandEvent context, (string, object) arg0, (string, object) arg1)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0, arg1);
    /// <inheritdoc cref="GetString(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, InteractHandEvent context, (string, object) arg0, (string, object) arg1)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0, arg1);

    /// <inheritdoc cref="GetString(String, object)"/>
    public static string GetString(string messageId, InteractHandEvent context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0, arg1, arg2);
    /// <inheritdoc cref="GetString(String, EntityUid, object)"/>
    public static string GetStringWithEntity(string messageId, EntityUid entity, InteractHandEvent context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), ("target", Identity.Entity(context.Target, _entity)), arg0, arg1, arg2);
}
