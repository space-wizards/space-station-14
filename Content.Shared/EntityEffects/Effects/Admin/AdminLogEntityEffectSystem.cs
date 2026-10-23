using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Shared.EntityEffects.Effects.Admin;

/// <summary>
/// Adds an admin log on effect.
/// </summary>
public sealed partial class AdminLogEntityEffectSystem : EntityEffectSystem<TransformComponent, AdminLog>
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<AdminLog> args)
    {
        if (args.User is {} user)
            _adminLogger.Add(args.Effect.LogType, args.Effect.Impact, $"{args.Effect.Message} - Entity: {ToPrettyString(entity.Owner):entity}, User: {ToPrettyString(user):player}");
        else
            _adminLogger.Add(args.Effect.LogType, args.Effect.Impact, $"{args.Effect.Message} - Entity: {ToPrettyString(entity.Owner):entity}");

    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdminLog : EntityEffectBase<AdminLog>
{
    [DataField(required: true)]
    public string Message = string.Empty;

    /// <summary>
    /// What kind of log type to use.
    /// </summary>
    [DataField]
    public new LogType LogType = LogType.Construction;

    /// <summary>
    /// What kind log impact.
    /// </summary>
    [DataField]
    public new LogImpact Impact = LogImpact.Medium;
}
