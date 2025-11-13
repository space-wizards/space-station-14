using Content.Shared.Interaction.Events;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    public static string GetString(string messageId, UseInHandEvent context)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)));
    public static string GetStringWithEntity(string messageId, EntityUid entity, UseInHandEvent context)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)));

    public static string GetString(string messageId, UseInHandEvent context, (string, object) arg0)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), arg0);
    public static string GetStringWithEntity(string messageId, EntityUid entity, UseInHandEvent context, (string, object) arg0)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), arg0);

    public static string GetString(string messageId, UseInHandEvent context, (string, object) arg0, (string, object) arg1)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), arg0, arg1);
    public static string GetStringWithEntity(string messageId, EntityUid entity, UseInHandEvent context, (string, object) arg0, (string, object) arg1)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), arg0, arg1);

    public static string GetString(string messageId, UseInHandEvent context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => _loc.GetString(messageId, ("user", Identity.Entity(context.User, _entity)), arg0, arg1, arg2);
    public static string GetStringWithEntity(string messageId, EntityUid entity, UseInHandEvent context, (string, object) arg0, (string, object) arg1, (string, object) arg2)
        => _loc.GetString(messageId, ("entity", Identity.Entity(entity, _entity)), ("user", Identity.Entity(context.User, _entity)), arg0, arg1, arg2);
}
