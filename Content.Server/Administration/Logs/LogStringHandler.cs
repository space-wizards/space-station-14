using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Content.Server.Administration.Logs.Converters;

namespace Content.Server.Administration.Logs;

[InterpolatedStringHandler]
public ref struct LogStringHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new EntityJsonConverter()
        }
    };

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

    public string ToJson()
    {
        return JsonSerializer.Serialize(_values, JsonOptions);
    }

    public string ToStringAndClear()
    {
        _values.Clear();
        return _handler.ToStringAndClear();
    }
}
