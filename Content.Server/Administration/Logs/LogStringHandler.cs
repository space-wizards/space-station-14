using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Administration.Logs;

[InterpolatedStringHandler]
public ref struct LogStringHandler
{
    private DefaultInterpolatedStringHandler _handler;
    private readonly Dictionary<string, object?> _values;

    public LogStringHandler(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _values = new Dictionary<string, object?>();
    }

    public LogStringHandler(int literalLength, int formattedCount, IFormatProvider? provider)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, provider);
        _values = new Dictionary<string, object?>();
    }

    public LogStringHandler(int literalLength, int formattedCount, IFormatProvider? provider, Span<char> initialBuffer)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, provider, initialBuffer);
        _values = new Dictionary<string, object?>();
    }

    private void AddFormat<T>(string? format, T value)
    {
        if (format != null)
        {
            _values.Add(format, value);
        }
    }

    public void AppendLiteral(string value)
    {
        _handler.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T value)
    {
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value, string? format)
    {
        AddFormat(format, value);
        _handler.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment)
    {
        _handler.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        AddFormat(format, value);
        _handler.AppendFormatted(value, alignment, format);
    }

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
        AddFormat(format, value);
        _handler.AppendFormatted(value, alignment, format);
    }

    public (JsonDocument json, List<Guid> players) ToJson(JsonSerializerOptions options, IEntityManager entityManager)
    {
        var entities = new List<int>();
        var players = new List<Guid>();

        foreach (var obj in _values.Values)
        {
            EntityUid? entityId = obj switch
            {
                EntityUid id => id,
                IEntity entity => entity.Uid,
                IPlayerSession {AttachedEntityUid: { }} session => session.AttachedEntityUid.Value,
                _ => null
            };

            if (entityId is not { } uid)
            {
                continue;
            }

            entities.Add((int) uid);

            if (entityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                players.Add(actor.PlayerSession.UserId.UserId);
            }
        }

        _values["__entities"] = entities;
        // _values["__players"] = players;

        return (JsonSerializer.SerializeToDocument(_values, options), players);
    }

    public string ToStringAndClear()
    {
        _values.Clear();
        return _handler.ToStringAndClear();
    }
}
