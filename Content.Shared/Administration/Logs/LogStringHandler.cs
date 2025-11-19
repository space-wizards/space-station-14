using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Administration.Logs;

[InterpolatedStringHandler]
[SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
public ref struct LogStringHandler
{
    public readonly ISharedAdminLogManager Logger;
    private DefaultInterpolatedStringHandler _handler;
    public readonly Dictionary<string, object?> Values;

    public LogStringHandler(int literalLength, int formattedCount, ISharedAdminLogManager logger, out bool isEnabled)
    {
        isEnabled = logger.Enabled;
        if (!isEnabled)
        {
            Values = default!;
            Logger = default!;
            return;
        }

        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);

        // TODO LOGGING Dictionary pool?
        Values = new Dictionary<string, object?>(formattedCount);
        Logger = logger;
    }

    private void AddFormat<T>(string? format, T value, string? argument = null)
    {
        if (format == null)
        {
            if (argument == null)
                return;

            format = argument[0] == '@' ? argument[1..] : argument;
        }

        format = Logger.ConvertName(format);
        if (Values.TryAdd(format, value)
            || Values[format] is T val && val.Equals(value) )
        {
            return;
        }

        var originalFormat = format;
        var i = 2;
        format = $"{originalFormat}_{i}";

        while (!(Values.TryAdd(format, value)
                 || Values[format] is T val2 && val2.Equals(value)))
        {
            format = $"{originalFormat}_{i}";
            i++;
        }
    }

    public void AppendLiteral(string value)
    {
        _handler.AppendLiteral(value);
    }

    #region EntityUid

    public void AppendFormatted(EntityUid value, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), argument);
    }

    public void AppendFormatted(EntityUid value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), format, argument);
    }

    public void AppendFormatted(EntityUid value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, argument);
    }

    public void AppendFormatted(EntityUid value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, format, argument);
    }

    public void AppendFormatted(EntityUid? value, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), argument);
    }

    public void AppendFormatted(EntityUid? value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), format, argument);
    }

    public void AppendFormatted(EntityUid? value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, argument);
    }

    public void AppendFormatted(EntityUid? value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, format, argument);
    }

    #endregion

    #region NetEntity

    public void AppendFormatted(NetEntity value, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), argument);
    }

    public void AppendFormatted(NetEntity value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), format, argument);
    }

    public void AppendFormatted(NetEntity value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, argument);
    }

    public void AppendFormatted(NetEntity value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, format, argument);
    }

    public void AppendFormatted(NetEntity? value, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), argument);
    }

    public void AppendFormatted(NetEntity? value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), format, argument);
    }

    public void AppendFormatted(NetEntity? value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, argument);
    }

    public void AppendFormatted(NetEntity? value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AppendFormatted(Logger.EntityManager.ToPrettyString(value), alignment, format, argument);
    }
    #endregion

    #region Player

    public void AppendFormatted(ICommonSession? value, [CallerArgumentExpression("value")] string? argument = null)
    {
        SerializablePlayer? player = value == null ? null : new(value, Logger.EntityManager);
        AddFormat(null, player, argument);
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted(ICommonSession? value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        SerializablePlayer? player = value == null ? null : new(value, Logger.EntityManager);
        AddFormat(null, player, argument);
        _handler.AppendFormatted(value, format);
    }

    public void AppendFormatted(ICommonSession? value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        SerializablePlayer? player = value == null ? null : new(value, Logger.EntityManager);
        AddFormat(null, player, argument);
        _handler.AppendFormatted(value, alignment);
    }

    public void AppendFormatted(ICommonSession? value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        SerializablePlayer? player = value == null ? null : new(value, Logger.EntityManager);
        AddFormat(null, player, argument);
        _handler.AppendFormatted(value, alignment, format);
    }
    #endregion

    #region Generic

    public void AppendFormatted<T>(T value, [CallerArgumentExpression("value")] string? argument = null)
    {
        if (value is IAsType<EntityUid> ent)
        {
            AppendFormatted(ent.AsType(), argument);
            return;
        }

        AddFormat(null, value, argument);
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        if (value is IAsType<EntityUid> ent)
        {
            AppendFormatted(ent.AsType(), format, argument);
            return;
        }

        AddFormat(format, value, argument);
        _handler.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        if (value is IAsType<EntityUid> ent)
        {
            AppendFormatted(ent.AsType(), alignment, argument);
            return;
        }

        AddFormat(null, value, argument);
        _handler.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        if (value is IAsType<EntityUid> ent)
        {
            AppendFormatted(ent.AsType(), alignment, format, argument);
            return;
        }

        AddFormat(format, value, argument);
        _handler.AppendFormatted(value, alignment, format);
    }

    #endregion

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        _handler.AppendFormatted(value);
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        AddFormat(format, value.ToString());
        _handler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(string? value)
    {
        _handler.AppendFormatted(value);
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
    {
        AddFormat(format, value);
        _handler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        AddFormat(null, value, format);
        _handler.AppendFormatted(value, alignment, format);
    }

    public string ToStringAndClear()
    {
        return _handler.ToStringAndClear();
    }
}

public readonly struct SerializablePlayer
{
    public readonly Guid UserId;
    public readonly EntityUid? Uid;
    public readonly string? Name;

    public SerializablePlayer(ICommonSession player, IEntityManager entityManager)
    {
        UserId = player.UserId.UserId;
        if (player.AttachedEntity is not {} uid)
            return;

        Uid = uid;
        Name = entityManager.GetComponentOrNull<MetaDataComponent>(uid)?.EntityName;
    }
}
