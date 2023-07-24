using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NewCon.Errors;
using Content.Server.NewCon.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server.NewCon;

public sealed partial class NewConManager
{
    private readonly Dictionary<Type, ITypeParser> _consoleTypeParsers = new();
    private readonly Dictionary<Type, Type> _genericTypeParsers = new();

    private void InitializeParser()
    {
        var parsers = _reflection.GetAllChildren<ITypeParser>();

        foreach (var parserType in parsers)
        {
            if (parserType.IsGenericType)
            {
                var t = parserType.BaseType!.GetGenericArguments().First();
                _genericTypeParsers.Add(t.GetGenericTypeDefinition(), parserType);
                Logger.Debug($"Setting up generic {parserType}, {t.GetGenericTypeDefinition()}");
            }
            else
            {
                var parser = (ITypeParser) _typeFactory.CreateInstance(parserType);
                Logger.Debug($"Setting up {parserType}, {parser.Parses}");
                _consoleTypeParsers.Add(parser.Parses, parser);
            }
        }
    }

    public ITypeParser? GetParserForType(Type t)
    {
        if (_consoleTypeParsers.TryGetValue(t, out var parser))
            return parser;

        if (t.IsConstructedGenericType)
        {
            if (!_genericTypeParsers.TryGetValue(t.GetGenericTypeDefinition(), out var genParser))
                return null;

            var concreteParser = genParser.MakeGenericType(t.GenericTypeArguments);

            var builtParser = (ITypeParser) _typeFactory.CreateInstance(concreteParser);
            _consoleTypeParsers.Add(builtParser.Parses, builtParser);
            return builtParser;
        }

        var baseTy = t.BaseType;

        if (baseTy is not null)
            return GetParserForType(t);

        return null;
    }

    public bool TryParse<T>(ForwardParser parser, [NotNullWhen(true)] out object? parsed, out IConError? error)
    {
        return TryParse(parser, typeof(T), out parsed, out error);
    }

    public bool TryParse(ForwardParser parser, Type t, [NotNullWhen(true)] out object? parsed, out IConError? error)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            parsed = null;
            error = new UnparseableValueError(t);
            return false;
        }

        return impl.TryParse(parser, out parsed, out error);
    }
}

public record struct UnparseableValueError(Type T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup($"The type {T.PrettyName()} has no parser available and cannot be parsed.");
        msg.AddText("Please contact a programmer with this error, they'd probably like to see it.");
        msg.AddMarkup("[bold][color=red]THIS IS A BUG.[/color][/bold]");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
