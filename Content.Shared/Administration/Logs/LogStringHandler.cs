using System.Runtime.CompilerServices;

namespace Content.Shared.Administration.Logs;

[InterpolatedStringHandler]
public ref struct LogStringHandler
{
    private DefaultInterpolatedStringHandler _handler;
    public readonly Dictionary<string, object?> Values;

    public LogStringHandler(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        Values = new Dictionary<string, object?>();
    }

    public LogStringHandler(int literalLength, int formattedCount, IFormatProvider? provider)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, provider);
        Values = new Dictionary<string, object?>();
    }

    public LogStringHandler(int literalLength, int formattedCount, IFormatProvider? provider, Span<char> initialBuffer)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, provider, initialBuffer);
        Values = new Dictionary<string, object?>();
    }

    private void AddFormat<T>(string? format, T value, string? argument = null)
    {
        if (format == null)
        {
            if (argument == null)
            {
                return;
            }

            format = argument[0] == '@' ? argument[1..] : argument;
        }

        if (Values.TryAdd(format, value) ||
            Values[format] == (object?) value)
        {
            return;
        }

        var originalFormat = format;
        var i = 2;
        format = $"{originalFormat}_{i}";

        while (!Values.TryAdd(format, value))
        {
            format = $"{originalFormat}_{i}";
            i++;
        }
    }

    public void AppendLiteral(string value)
    {
        _handler.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T value, [CallerArgumentExpression("value")] string? argument = null)
    {
        AddFormat(null, value, argument);
        _handler.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AddFormat(format, value, argument);
        _handler.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment, [CallerArgumentExpression("value")] string? argument = null)
    {
        AddFormat(null, value, argument);
        _handler.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T value, int alignment, string? format, [CallerArgumentExpression("value")] string? argument = null)
    {
        AddFormat(format, value, argument);
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
        AddFormat(null, value, format);
        _handler.AppendFormatted(value, alignment, format);
    }

    public string ToStringAndClear()
    {
        Values.Clear();
        return _handler.ToStringAndClear();
    }
}
